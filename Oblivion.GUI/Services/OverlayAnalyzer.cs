using System.IO;

namespace Oblivion.GUI.Services
{
    public static class OverlayAnalyzer
    {
        public static void ExtractOverlay(string sourceFilePath, long overlayOffset, long overlaySize, string outputPath)
        {
            using var input = File.OpenRead(sourceFilePath);
            input.Seek(overlayOffset, SeekOrigin.Begin);

            using var output = File.Create(outputPath);

            byte[] buffer = new byte[81920];
            long remaining = overlaySize;

            while (remaining > 0)
            {
                int toRead = (int)System.Math.Min(buffer.Length, remaining);
                int read = input.Read(buffer, 0, toRead);
                if (read == 0) break;
                output.Write(buffer, 0, read);
                remaining -= read;
            }
        }
    }
}
