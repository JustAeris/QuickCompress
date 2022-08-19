using QuickCompress.Core.Common;

namespace QuickCompress.Gui.Models;

public record VideoOptions(bool IsCRF, double CRF, bool IsResizing, bool IsPercentage, double Percentage,
    bool IsAbsoluteSize, double AbsoluteSize, bool TwoPasses, FFmpegSpeed Speed);
