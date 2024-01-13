using ContentSafetyTest;


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
        var unsafeRangesDictionary = imageContentAnalyzer.GetUnsafeRanges("gta_small.mp4", 1f);
        Console.WriteLine("Finished analyzing.\n");

        Console.WriteLine("Writing unsafe frames to a text file...");
        using StreamWriter sw = new("unsafe_frames.txt");
        foreach (var (category, ranges) in unsafeRangesDictionary)
        {
            sw.WriteLine($"{category}:");
            foreach (var range in ranges)
            {
                sw.WriteLine(range);
            }
            sw.WriteLine();
        }
        Console.WriteLine("Done.\n");
    }
}