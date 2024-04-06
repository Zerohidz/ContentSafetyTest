using VideoContentAnalyzer;


internal class Program
{
    private static void Main(string[] args)
    {
        string endpoint = Environment.GetEnvironmentVariable("CONTENT_SAFETY_ENDPOINT")
            ?? throw new InvalidOperationException("Endpoint Not Found In Environment Variables!");
        string apiKey = Environment.GetEnvironmentVariable("CONTENT_SAFETY_KEY")
            ?? throw new InvalidOperationException("Api Key Not Found In Environment Variables!");

        //////////////////////////

        string? videoPath = GetPathFromUser();

        //////////////////////////

        //string _framesDirectory = Path.Combine(Environment.CurrentDirectory, "frames");
        //VideoProcessor.SaveFramesFromVideo(videoPath, _framesDirectory, 1);

        //////////////////////////

        ImageContentAnalyzer analyzer = new(endpoint, apiKey);
        var unsafeRanges = analyzer.GetUnsafeRanges(videoPath, 1);
        analyzer.SaveUnsafeRangesToFile(unsafeRanges, $"Unsafe Ranges.txt");
    }

    private static string GetPathFromUser()
    {
        string? videoPath;
        while (true)
        {
            Console.WriteLine("Enter Video Path:");
            videoPath = Console.ReadLine();
            if (!File.Exists(videoPath))
                Console.WriteLine("Invalid Path!");
            else
                break;
        }

        return videoPath;
    }
}