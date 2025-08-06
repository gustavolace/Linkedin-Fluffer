using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace LinkedinFluffer.Services
{
    public class IMGGenerator
    {
        private readonly HttpClient _client;
        private readonly string _accountId;

        public IMGGenerator(string accountId, string apiToken)
        {
            _accountId = accountId;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        }

        public async Task<byte[]> GenerateImageAsync(string prompt)
        {
            var url = $"https://api.cloudflare.com/client/v4/accounts/{_accountId}/ai/run/@cf/stabilityai/stable-diffusion-xl-base-1.0";
            var requestBody = new { prompt };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"Image generation failed: {response.StatusCode}\n{errorText}");
            }

            // Cloudflare returns binary PNG, so just read as bytes
            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            // Optional: resize/crop
            using var image = Image.Load(imageBytes);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(708, 370),
                Mode = ResizeMode.Crop
            }));

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }
    }
}
