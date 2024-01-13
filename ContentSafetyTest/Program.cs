using ContentSafetyTest;

// TODO: range bilgisi yap

// DONE: onlyCheckForSexuality yerine hangi aralıkta ne var onu yazdır

internal class Program
{
    private static void Main(string[] args)
    {
        string endpoint = Environment.GetEnvironmentVariable("CONTENT_SAFETY_ENDPOINT")
            ?? throw new InvalidOperationException("Endpoint Not Found In Environment Variables!");
        string apiKey = Environment.GetEnvironmentVariable("CONTENT_SAFETY_KEY")
            ?? throw new InvalidOperationException("Api Key Not Found In Environment Variables!");

        ImageContentAnalyzer imageContentAnalyzer = new(endpoint, apiKey);

        Console.WriteLine("Analyzing the video...");
        var unsafeFrames = imageContentAnalyzer.GetUnsafeTimestamps("gta_small.mp4", 1f);
        Console.WriteLine("Finished analyzing.\n");

        Console.WriteLine("Writing unsafe frames to a text file...");
        File.WriteAllLines("unsafeFrames.txt", unsafeFrames.Select(f => f.ToString()));
        Console.WriteLine("Done.\n");
    }
}