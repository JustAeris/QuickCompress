using QuickCompress.Core.VideoCompression;

namespace QuickCompress.Gui.Models.VideoCompression;

public record Options(bool IsCRF, double CRF, bool IsResizing, bool IsPercentage, double Percentage, bool IsAbsoluteSize, double AbsoluteSize, bool TwoPasses, FFmpegSpeed Speed)
{
    public bool IsCRF { get; } = IsCRF;
    public double CRF { get; } = CRF;

    public bool IsResizing { get; } = IsResizing;
    public bool IsPercentage { get; } = IsPercentage;
    public double Percentage { get; } = Percentage;
    public bool IsAbsoluteSize { get; } = IsAbsoluteSize;
    public double AbsoluteSize { get; } = AbsoluteSize;

    public bool TwoPasses { get; } = TwoPasses;
    public FFmpegSpeed Speed { get; } = Speed;
}
