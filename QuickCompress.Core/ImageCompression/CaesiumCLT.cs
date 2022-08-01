using System.Diagnostics;

namespace QuickCompress.Core.ImageCompression;

public static class CaesiumCLT
{
    private static readonly Process CaesiumProcess = new()
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "Tools/Caesium/caesiumclt.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        }
    };

    /// <summary>
    /// Utilize the CaesiumCLT to compress an image.
    /// </summary>
    /// <param name="inputFile">File to compress, can be a JPG or PNG.</param>
    /// <param name="outputFolder">Path to the output folder where the compressed files will be stored. Can be the same input folder, which will overwrite the original files.</param>
    /// <param name="quality">Sets the quality of the image. The higher the value is, better the result will be. Note that a value of 0 will mean lossless compression, which will not modify the original image, but will compress less. Allowed range is [0. 100].A common value for lossy compression is 80.</param>
    /// <param name="keepExif">Keeps the JPEG metadata information during compression. File size will be slightly higher.</param>
    /// <param name="overwriteMode">Sets the overwrite policy: <see cref="OverwriteMode.All"/> will overwrite any existing file, <see cref="OverwriteMode.Bigger"/> will overwrite bigger files only, and <see cref="OverwriteMode.None"/> will silently skip existing files.</param>
    /// <exception cref="ArgumentOutOfRangeException">Quality is not between 0 and a 100</exception>
    /// <exception cref="FileNotFoundException">File could not be found</exception>
    public static void CompressImage(FileInfo inputFile, string outputFolder, int quality = 80, bool keepExif = true,
        OverwriteMode overwriteMode = OverwriteMode.Bigger)
    {
        if (quality is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 100.");
        if (!File.Exists(inputFile.FullName))
            throw new FileNotFoundException("Input file not found.", nameof(inputFile));

        var arguments = $"-q {quality} {(keepExif ? "-e " : null)} -O {Enum.GetName(overwriteMode)} -o {outputFolder} {inputFile}";
        CaesiumProcess.StartInfo.Arguments = arguments;
        CaesiumProcess.Start();
        CaesiumProcess.WaitForExit();
    }
}
