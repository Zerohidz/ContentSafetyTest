using Emgu.CV;

namespace ContentSafetyTest;

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

        int i = 1;
        while (video.Grab())
        {
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
    }
}
