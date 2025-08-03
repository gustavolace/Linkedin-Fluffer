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
        string cloudflareAccountId = Environment.GetEnvironmentVariable("CLOUDFLARE_ACCOUNT_ID")!;
        string cloudflareToken = Environment.GetEnvironmentVariable("CLOUDFLARE_API_TOKEN")!;

        var generator = new Generator();
        var imgGen = new IMGGenerator(cloudflareAccountId, cloudflareToken);
        var poster = new Poster(token, urn);

        while (true)
        {
            string generatedText = await generator.GenerateTextAsync();
            byte[] imgBytes = await imgGen.GenerateImageAsync(generatedText);
            await poster.PostAsync(generatedText, imgBytes);

            Console.WriteLine("✅ Posted to LinkedIn, waiting 1 day...");
            await Task.Delay(TimeSpan.FromDays(1)); // Wait 1 day before next post
        }
    }
}
