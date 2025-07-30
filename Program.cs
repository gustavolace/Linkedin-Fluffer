using System.Threading.Tasks;
using LinkedinFluffer.Services;

class Program
{
    static async Task Main()
    {
        var generator = new Generator();
        await generator.RunAsync();
    }
}
