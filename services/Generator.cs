using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotenv.net;

namespace LinkedinFluffer.Services
{
    public class Generator
    {
        private readonly HttpClient _client;
        private readonly List<object> _messages;

        public Generator()
        {
            var apiKey = Environment.GetEnvironmentVariable("API_KEY")!;
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://openrouter.ai/api/v1/")
            };
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            _messages = new List<object>
            {
                new { role = "system", content = "You are a helpful assistant." }
            };
        }
        private async Task<string> SendChatRequest(List<object> messages)
        {
            var requestBody = new
            {
                model = "deepseek/deepseek-r1:free",
                messages
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"API request failed: {response.StatusCode}\n{responseString}");

            using var doc = JsonDocument.Parse(responseString);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }

        public async Task<string> GenerateTextAsync()
        {
            string prompt = "Write other a short engaging post for LinkedIn about technology trends."; // or whatever prompt you want
            _messages.Add(new { role = "user", content = prompt });
            string reply = await SendChatRequest(_messages);
            _messages.Add(new { role = "assistant", content = reply });
            return reply;
        }

    }
}
