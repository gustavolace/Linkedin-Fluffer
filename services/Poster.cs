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
            _personUrn = personUrn;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);
            _client.DefaultRequestHeaders.Add("LinkedIn-Version", "202210");
            _client.DefaultRequestHeaders.Add("X-Restli-Protocol-Version", "2.0.0");
        }

        public async Task PostAsync(string text)
        {
            var postBody = new
            {
                author = $"urn:li:person:{_personUrn.Trim()}",
                lifecycleState = "PUBLISHED",
                specificContent = new
                {
                    comlinkedinugcShareContent = new
                    {
                        shareCommentary = new
                        {
                            text = text
                        },
                        shareMediaCategory = "NONE"
                    }
                },
                visibility = new
                {
                    comlinkedinugcMemberNetworkVisibility = "PUBLIC"
                }
            };

            // LinkedIn API expects dots in keys; fix after serialization
            string json = JsonSerializer.Serialize(postBody)
                .Replace("comlinkedinugcShareContent", "com.linkedin.ugc.ShareContent")
                .Replace("comlinkedinugcMemberNetworkVisibility", "com.linkedin.ugc.MemberNetworkVisibility");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("https://api.linkedin.com/v2/ugcPosts", content);

            Console.WriteLine("LinkedIn Status: " + response.StatusCode);
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
}
