using QuickCompress.Core.Common;

namespace QuickCompress.Gui.Models;

public record AudioOptions(bool IsSetBitrate, int Bitrate, bool IsResizing, bool IsPercentage, double Percentage,
    bool IsAbsoluteSize, double AbsoluteSize, FFmpegSpeed Speed);
