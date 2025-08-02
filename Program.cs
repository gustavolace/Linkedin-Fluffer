using System;
using System.Threading.Tasks;
using LinkedinFluffer.Services;
using dotenv.net;

class Program
{
    static async Task Main()
    {
        DotEnv.Load();

        string token = Environment.GetEnvironmentVariable("LINKEDIN_TOKEN")!;
        string urn = Environment.GetEnvironmentVariable("URN")!;

        var generator = new Generator();
        var poster = new Poster(token, urn);

        while (true)
        {
            string generatedText = await generator.GenerateTextAsync(); // Assuming you have refactored RunAsync to just generate once
            await poster.PostAsync(generatedText);

            Console.WriteLine("✅ Posted to LinkedIn, waiting 1 day...");

            await Task.Delay(TimeSpan.FromDays(1)); // Wait 1 day before next post
        }
    }
}
