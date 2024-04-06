using Azure;
using Azure.AI.ContentSafety;
using Emgu.CV.Structure;
using Emgu.CV;

namespace VideoContentAnalyzer;

internal record ImageContentAnalyzer
{
    private static readonly string _framesDirectory = Path.Combine(Environment.CurrentDirectory, "frames");
    private static readonly ImageCategory[] s_ImageCategories = [ImageCategory.Violence, ImageCategory.SelfHarm, ImageCategory.Sexual, ImageCategory.Hate];

    private string _endpoint;
    private string _apiKey;
    private ContentSafetyClient _client;


    public ImageContentAnalyzer(string endpoint, string apiKey)
    {
        _endpoint = endpoint;
        _apiKey = apiKey;
        _client = new(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public IReadOnlyList<ImageCategoriesAnalysis>? IsSafe(string? imagePath)
    {
        if (imagePath == null) throw new("Image Path Null!");

        Image<Bgr, byte> img = GetDownscaledImage(imagePath);
        ContentSafetyImageData image = new(BinaryData.FromBytes(img.ToJpegData()));
        AnalyzeImageOptions analyzeOptions = new(image);

        try
        {
            var response = _client.AnalyzeImage(analyzeOptions);
            return response.Value.CategoriesAnalysis;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Analyze image failed.\nError message: {0}", ex.Message);
            return null;
        }
    }

    public async Task<IReadOnlyList<ImageCategoriesAnalysis>?> GetAnalysisAsync(string? imagePath)
    {
        if (imagePath == null) throw new("Image Path Null!");

        Image<Bgr, byte> img = GetDownscaledImage(imagePath);
        ContentSafetyImageData image = new(BinaryData.FromBytes(img.ToJpegData()));
        AnalyzeImageOptions analyzeOptions = new(image);

        try
        {
            var response = await _client.AnalyzeImageAsync(analyzeOptions);

            return response.Value.CategoriesAnalysis;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Analyze image failed.\nError message: {0}", ex.Message);
            return null;
        }
    }

    public Dictionary<ImageCategory, List<string>> GetUnsafeRanges(string videoPath, float ssPerSeconds)
    {
        if (!File.Exists(videoPath))
            throw new("Video Path Not Found!");
        VideoProcessor.SaveFramesFromVideo(videoPath, _framesDirectory, ssPerSeconds);

        string[] frames = Directory.GetFiles(_framesDirectory, "*.png");

        Console.WriteLine("Starting to send to server...");
        List<Task<IReadOnlyList<ImageCategoriesAnalysis>?>> results = new();
        for (int i = 0; i < frames.Length; i++)
        {
            Thread.Sleep(200);
            Console.WriteLine(i + 1 + " images sent...");

            results.Add(GetAnalysisAsync(frames[i]));
        }
        Task.WaitAll(results.ToArray());
        Directory.Delete(_framesDirectory, true);

        Dictionary<ImageCategory, List<string>> unsafeRanges = s_ImageCategories.Select(c => (c, new List<string>())).ToDictionary();
        string? rangeStart = null;
        string? lastFrame = null;
        foreach (ImageCategory categoryKind in s_ImageCategories)
        {
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i].Result;
                string frameName = Path.GetFileNameWithoutExtension(frames[i]);

                if (result == null)
                    continue;

                bool isUnsafe = result.Where(c => c.Category == categoryKind).FirstOrDefault()?.Severity!.Value > 0;

                if (isUnsafe)
                {
                    rangeStart ??= frameName;
                }
                else
                {
                    if (rangeStart != null)
                    {
                        if (rangeStart == lastFrame)
                            unsafeRanges[categoryKind].Add(rangeStart + ": " + categoryKind.ToString());
                        else
                            unsafeRanges[categoryKind].Add(rangeStart + " -> " + lastFrame + ": " + categoryKind.ToString());

                        rangeStart = null;
                    }
                }
                lastFrame = frameName;
            }
        }

        return unsafeRanges;
    }

    public void SaveUnsafeRangesToFile(Dictionary<ImageCategory, List<string>> unsafeRanges, string outputPath)
    {
        using StreamWriter sw = new(outputPath);
        foreach (var (category, ranges) in unsafeRanges)
        {
            sw.WriteLine(category.ToString());
            foreach (string range in ranges)
            {
                sw.WriteLine(range);
            }
            sw.WriteLine();
        }

        Console.WriteLine("Unsafe ranges saved to file.");
    }

    private static Image<Bgr, byte> GetDownscaledImage(string? imagePath)
    {
        Image<Bgr, byte> img = new(imagePath);
        if (img.Width > 2048 || img.Height > 2048)
        {
            double factor = Math.Min(2048.0 / img.Width, 2048.0 / img.Height);
            int newWidth = (int)(img.Width * factor);
            int newHeight = (int)(img.Height * factor);

            img = img.Resize(newWidth, newHeight, Emgu.CV.CvEnum.Inter.Linear);
        }

        return img;
    }
}
