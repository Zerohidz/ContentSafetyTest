using Emgu.CV;

namespace VideoContentAnalyzer;

internal static class VideoProcessor
{
    public static void SaveFramesFromVideo(string videoPath, string outputDir, float ssPerSecond = 1)
    {
        if (Directory.Exists(outputDir))
            Directory.Delete(outputDir, true);
        Directory.CreateDirectory(outputDir);

        using var video = new VideoCapture(videoPath);
        using var img = new Mat();

        double fps = video.Get(Emgu.CV.CvEnum.CapProp.Fps);
        Console.WriteLine("The video fps is: " + MathF.Round((float)fps, 2) + " (Check if it is correct or not)");
        Console.WriteLine("Starting to save frames from the video.");

        int i = 1;
        while (video.Grab())
        {
            Console.WriteLine($"Processed the video until timestamp: {TimeSpan.FromSeconds(i / fps).ToString("hh\\:mm\\:ss\\.ff")}");

            video.Retrieve(img);
            string filename = Path.Combine(outputDir, $"{TimeSpan.FromSeconds(i / fps):hh\\-mm\\-ss\\.ff}.png");
            CvInvoke.Imwrite(filename, img);
            i++;

            int framesToskip = (int)(fps / ssPerSecond - 1);
            for (int j = 0; j < framesToskip; j++)
            {
                video.Grab();
                i++;
            }
        }

        Console.WriteLine("Finished saving frames from the video.");
    }
}
