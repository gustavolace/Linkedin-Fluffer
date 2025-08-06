using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LinkedinFluffer.Services
{
    public class Poster
    {
        private readonly string _accessToken;
        private readonly string _personUrn;
        private readonly HttpClient _client;

        public Poster(string accessToken, string personUrn)
        {
            _accessToken = accessToken;
            _personUrn = personUrn.Trim();
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            _client.DefaultRequestHeaders.Add("LinkedIn-Version", "202210");
            _client.DefaultRequestHeaders.Add("X-Restli-Protocol-Version", "2.0.0");
        }

        public async Task PostAsync(string text, byte[] imgBytes)
        {
            string cleanText = text.Replace("**", "");

            // 1️⃣ Register image upload
            var registerBody = new
            {
                registerUploadRequest = new
                {
                    owner = $"urn:li:person:{_personUrn}",
                    recipes = new[] { "urn:li:digitalmediaRecipe:feedshare-image" },
                    serviceRelationships = new[]
                    {
                        new {
                            relationshipType = "OWNER",
                            identifier = "urn:li:userGeneratedContent"
                        }
                    },
                    supportedUploadMechanism = new[] { "SYNCHRONOUS_UPLOAD" }
                }
            };

            var registerResponse = await _client.PostAsync(
                "https://api.linkedin.com/v2/assets?action=registerUpload",
                new StringContent(JsonSerializer.Serialize(registerBody), Encoding.UTF8, "application/json")
            );

            string registerJson = await registerResponse.Content.ReadAsStringAsync();
            if (!registerResponse.IsSuccessStatusCode)
                throw new Exception($"LinkedIn registerUpload failed: {registerResponse.StatusCode}\n{registerJson}");

            using var registerDoc = JsonDocument.Parse(registerJson);
            string uploadUrl = registerDoc.RootElement
                .GetProperty("value")
                .GetProperty("uploadMechanism")
                .GetProperty("com.linkedin.digitalmedia.uploading.MediaUploadHttpRequest")
                .GetProperty("uploadUrl").GetString();

            string mediaUrn = registerDoc.RootElement
                .GetProperty("value")
                .GetProperty("asset").GetString();

            // 2️⃣ Upload image
            using (var imgContent = new ByteArrayContent(imgBytes))
            {
                imgContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                var uploadResp = await _client.PutAsync(uploadUrl, imgContent);
                if (!uploadResp.IsSuccessStatusCode)
                {
                    var err = await uploadResp.Content.ReadAsStringAsync();
                    throw new Exception($"Image upload failed: {uploadResp.StatusCode}\n{err}");
                }
            }

            // 3️⃣ Post with UGC API
            var postBody = new
            {
                author = $"urn:li:person:{_personUrn}",
                lifecycleState = "PUBLISHED",
                specificContent = new
                {
                    comlinkedinugcShareContent = new
                    {
                        shareCommentary = new { text = cleanText },
                        shareMediaCategory = "IMAGE",
                        media = new[]
                        {
                            new { status = "READY", media = mediaUrn }
                        }
                    }
                },
                visibility = new
                {
                    comlinkedinugcMemberNetworkVisibility = "PUBLIC"
                }
            };

            string json = JsonSerializer.Serialize(postBody)
                .Replace("comlinkedinugcShareContent", "com.linkedin.ugc.ShareContent")
                .Replace("comlinkedinugcMemberNetworkVisibility", "com.linkedin.ugc.MemberNetworkVisibility");

            var response = await _client.PostAsync(
                "https://api.linkedin.com/v2/ugcPosts",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            string postResult = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"LinkedIn Post Status: {response.StatusCode}");
            Console.WriteLine(postResult);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Post failed: {response.StatusCode}\n{postResult}");
        }
    }
}
