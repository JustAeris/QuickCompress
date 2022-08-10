using System.Diagnostics.CodeAnalysis;

namespace QuickCompress.Core.Configuration;

[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
public class Options : Context
{
    private ThemeType _theme;

    public Options(VideoCompressionOptions videoCompressionOptions)
    {
        VideoCompressionOptions = videoCompressionOptions;
    }

    /// <summary>
    /// The theme to use for the application
    /// </summary>
    public ThemeType Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            SaveOptions();
        }
    }

    public VideoCompressionOptions VideoCompressionOptions { get; }
}
