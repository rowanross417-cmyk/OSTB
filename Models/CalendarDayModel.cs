namespace OSTB.Models;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class CalendarDayModel : ObservableObject
{
    public int DayNumber { get; set; }
    public string DateKey { get; set; } = string.Empty;
    public bool IsCurrentMonth { get; set; } = true;
    public bool IsToday { get; set; } // Highlight flag
    public double CellOpacity { get; set; } = 1.0;

    [ObservableProperty] private string _notesPreview = string.Empty;
    [ObservableProperty] private bool _hasNotes;
}