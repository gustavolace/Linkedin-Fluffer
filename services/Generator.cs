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
            DotEnv.Load();
            var apiKey = DotEnv.Read()["API_KEY"];

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

        public async Task RunAsync()
        {
            // First prompt
            string firstPrompt = "Generate a fluffy post for LinkedIn about technology";
            Console.WriteLine("You: " + firstPrompt);

            _messages.Add(new { role = "user", content = firstPrompt });
            string reply = await SendChatRequest(_messages);
            Console.WriteLine($"Assistant: {reply}");
            _messages.Add(new { role = "assistant", content = reply });

            // Loop for more
            while (true)
            {
                Console.Write("You: ");
                string? userInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userInput)) break;

                _messages.Add(new { role = "user", content = userInput });
                reply = await SendChatRequest(_messages);
                Console.WriteLine($"Assistant: {reply}");
                _messages.Add(new { role = "assistant", content = reply });
            }
        }

        private async Task<string> SendChatRequest(List<object> messages)
        {
            var requestBody = new
            {
                model = "deepseek/deepseek-r1:free",
                messages = messages
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("chat/completions", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"API request failed: {response.StatusCode}\n{errorText}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var contentResponse = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return contentResponse ?? "";
        }
    }
}
