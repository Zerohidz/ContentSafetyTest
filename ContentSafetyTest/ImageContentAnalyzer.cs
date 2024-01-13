using Azure;
using Azure.AI.ContentSafety;
using Emgu.CV.Structure;
using Emgu.CV;

namespace ContentSafetyTest;

internal record ImageContentAnalyzer
{
    private static string _framesDirectory = Path.Combine(Environment.CurrentDirectory, "frames");

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

    public string[] GetUnsafeTimestamps(string videoPath, float ssPerSeconds)
    {
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
        //Directory.Delete(_framesDirectory, true);

        List<string> unsafeFrames = new();
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i].Result;
            string frameName = Path.GetFileNameWithoutExtension(frames[i]);

            string description = " Resim işlenirken hata oluştu. Detaylı bilgi için console'a bakın.";
            if (result != null)
            {
                description = string.Empty;
                foreach (var category in result)
                {
                    if (category.Severity!.Value > 0)
                        description = " " + category.Category.ToString() + ": " + category.Severity.Value;
                }
            }

            if (description != string.Empty)
                unsafeFrames.Add(frameName + description);
        }

        return unsafeFrames.ToArray();
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
