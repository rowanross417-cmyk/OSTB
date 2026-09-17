using System.Collections.Generic;

namespace OSTB.Models;

public class AppData
{
    public double ElapsedSeconds { get; set; }
    public double BankedSeconds { get; set; }
    public double ConversionRatio { get; set; } = 1.0;
    public double TargetConversionMinutes { get; set; } = 5.0;
    public string NoteText { get; set; } = string.Empty;
    public bool IsFullscreen { get; set; } = false;
    public bool IsTopmost { get; set; } = false;
    public double AppScale { get; set; } = 1.0;
    public double NotepadFontSize { get; set; } = 14.0;
    public Dictionary<string, string> CalendarNotes { get; set; } = new();
}