namespace QuickCompress.Gui.Models;

public record BitratePreset(int Bitrate)
{
    public string Name => Bitrate == -1 ? "Custom" : $"{Bitrate}kbps";
}
