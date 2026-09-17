using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSTB.Models;

namespace OSTB.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DispatcherTimer _timer;
    private DateTime _lastTickTime;

    private TimeSpan _elapsedTime = TimeSpan.Zero;
    private TimeSpan _bankedTime = TimeSpan.Zero;
    private TimeSpan _initialEarnedTimeForConversion = TimeSpan.Zero;

    private bool _isRunning;
    private bool _isConverting;
    private bool _isClaiming;
    private bool _isConfirmingReset;
    private bool _isConfirmingBurn;

    // Generated Observable Properties
    [ObservableProperty] private string _timeDisplay = "00:00:00";
    [ObservableProperty] private string _bankedTimeDisplay = "00:00:00";
    [ObservableProperty] private string _startStopButtonText = "Start";
    [ObservableProperty] private string _startStopButtonDepressedColour = "#32a956";
    [ObservableProperty] private string _convertButtonText = "Convert";
    [ObservableProperty] private string _claimButtonText = "Start Claim";
    [ObservableProperty] private string _claimButtonColour = "#32a956";
    [ObservableProperty] private string _resetButtonText = "Reset";
    [ObservableProperty] private string _resetButtonColour = "#9B7EDE";

    [ObservableProperty] private double _conversionProgress;
    [ObservableProperty] private double _conversionRatio = 1.0;
    [ObservableProperty] private double _targetConversionMinutes = 5.0;

    [ObservableProperty] private bool _isBurnModalOpen;
    [ObservableProperty] private double _minutesToBurnInput = 15;
    [ObservableProperty] private string _confirmBurnButtonText = "Burn Time";
    [ObservableProperty] private string _confirmBurnButtonColour = "#e63946";

    [ObservableProperty] private string _noteText = string.Empty;
    [ObservableProperty] private bool _isFullscreen;
    [ObservableProperty] private bool _isTopmost;
    [ObservableProperty] private double _appScale = 1.0;
    [ObservableProperty] private double _notepadFontSize = 14.0;

    // Calendar Properties & Tracking
    [ObservableProperty] private ObservableCollection<CalendarDayModel> _calendarDays = new();
    [ObservableProperty] private string _currentMonthYearLabel = string.Empty;
    [ObservableProperty] private bool _isCalendarModalOpen;
    [ObservableProperty] private string _selectedDateLabel = "00/00/0000";
    [ObservableProperty] private string _selectedDayNoteText = string.Empty;

    private int _displayYear = DateTime.Now.Year;
    private int _displayMonth = DateTime.Now.Month;
    private string _selectedDateKey = string.Empty;
    private Dictionary<string, string> _calendarNotes = new();

    public MainViewModel()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _timer.Tick += Timer_Tick;

        IsCalendarModalOpen = false;
        IsBurnModalOpen = false;

        // 1. Load state FIRST so _calendarNotes is populated from storage
        LoadState();
        
        // 2. Then build the calendar using the loaded notes
        BuildCalendar(_displayYear, _displayMonth);
    }

    #region Property Changed Partial Hooks

    partial void OnConversionRatioChanged(double value) => SaveState();
    partial void OnTargetConversionMinutesChanged(double value) => SaveState();
    partial void OnNoteTextChanged(string value) => SaveState();

    #endregion

    #region Window Commands

    [RelayCommand]
    public void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
    }

    [RelayCommand]
    public void ToggleTopmost()
    {
        IsTopmost = !IsTopmost;
    }

    #endregion

    #region Scale & Notepad Commands

    [RelayCommand]
    public void IncreaseScale() => AppScale = Math.Min(3.0, AppScale + 0.1);

    [RelayCommand]
    public void DecreaseScale() => AppScale = Math.Max(0.5, AppScale - 0.1);

    [RelayCommand]
    public void ResetScale() => AppScale = 1.0;

    [RelayCommand]
    public void IncreaseNotepadFont() => NotepadFontSize = Math.Min(48.0, NotepadFontSize + 1.0);

    [RelayCommand]
    public void DecreaseNotepadFont() => NotepadFontSize = Math.Max(6.0, NotepadFontSize - 1.0);

    [RelayCommand]
    public void ResetNotepadFont() => NotepadFontSize = 14.0;

    #endregion

    #region Calendar Logic & Commands

    public void BuildCalendar(int year, int month)
    {
        CalendarDays.Clear();
        DateTime firstOfMonth = new DateTime(year, month, 1);
        CurrentMonthYearLabel = firstOfMonth.ToString("MMMM yyyy").ToUpper();

        DateTime firstVisibleDate = firstOfMonth.AddDays(-((int)firstOfMonth.DayOfWeek));
        DateTime today = DateTime.Today;

        for (int i = 0; i < 42; i++)
        {
            DateTime cellDate = firstVisibleDate.AddDays(i);
            string key = cellDate.ToString("yyyy-MM-dd");
            _calendarNotes.TryGetValue(key, out string? existingNote);

            bool isCurrentMonth = cellDate.Month == month;
            bool isToday = (cellDate.Year == today.Year && cellDate.Month == today.Month && cellDate.Day == today.Day);

            CalendarDays.Add(new CalendarDayModel
            {
                DayNumber = cellDate.Day,
                DateKey = key,
                IsCurrentMonth = isCurrentMonth,
                IsToday = isToday,
                NotesPreview = existingNote ?? string.Empty,
                HasNotes = !string.IsNullOrWhiteSpace(existingNote),
                CellOpacity = isCurrentMonth ? 1.0 : 0.45
            });
        }
    }

    [RelayCommand]
    public void PreviousMonth()
    {
        _displayMonth--;
        if (_displayMonth < 1)
        {
            _displayMonth = 12;
            _displayYear--;
        }
        BuildCalendar(_displayYear, _displayMonth);
    }

    [RelayCommand]
    public void NextMonth()
    {
        _displayMonth++;
        if (_displayMonth > 12)
        {
            _displayMonth = 1;
            _displayYear++;
        }
        BuildCalendar(_displayYear, _displayMonth);
    }

    [RelayCommand]
    public void OpenDayModal(CalendarDayModel day)
    {
        if (day == null || !day.IsCurrentMonth) return;

        _selectedDateKey = day.DateKey;
        if (DateTime.TryParse(day.DateKey, out DateTime parsedDate))
        {
            SelectedDateLabel = parsedDate.ToString("MM/dd/yyyy");
        }

        _calendarNotes.TryGetValue(_selectedDateKey, out string? note);
        SelectedDayNoteText = note ?? string.Empty;

        IsCalendarModalOpen = true;
    }

    [RelayCommand]
    public void SaveCalendarNote()
    {
        string noteText = SelectedDayNoteText ?? string.Empty;

        if (string.IsNullOrWhiteSpace(noteText))
        {
            _calendarNotes.Remove(_selectedDateKey);
        }
        else
        {
            _calendarNotes[_selectedDateKey] = noteText;
        }

        // Find and update the active day model in the collection
        foreach (var day in CalendarDays)
        {
            if (day.DateKey == _selectedDateKey)
            {
                day.NotesPreview = noteText;
                day.HasNotes = !string.IsNullOrWhiteSpace(noteText);
                break;
            }
        }

        SelectedDayNoteText = noteText;
        IsCalendarModalOpen = false;
        SaveState(); // Saves the updated _calendarNotes dictionary to storage
    }

    [RelayCommand]
    public void CloseCalendarModal()
    {
        IsCalendarModalOpen = false;
    }

    #endregion

    #region Button Commands

    [RelayCommand]
    private void StartStop()
    {
        if (_isRunning)
        {
            StartStopButtonText = "Start";
            StartStopButtonDepressedColour = "#32a956";
            _isRunning = false;
            CheckTimerState();
            SaveState();
        }
        else
        {
            if (_isConverting) ToggleConversion();
            if (_isClaiming) ToggleClaim();

            _lastTickTime = DateTime.Now;
            StartStopButtonText = "Stop";
            StartStopButtonDepressedColour = "#e63946";
            _isRunning = true;
            if (!_timer.IsEnabled) _timer.Start();
        }
    }

    [RelayCommand]
    private void ToggleConversion()
    {
        if (_isConverting)
        {
            _isConverting = false;
            ConvertButtonText = "Convert";
            ConversionProgress = 0;
            CheckTimerState();
            SaveState();
        }
        else
        {
            if (_isRunning || _elapsedTime <= TimeSpan.Zero) return;
            if (_isClaiming) ToggleClaim();

            _initialEarnedTimeForConversion = _elapsedTime;
            _lastTickTime = DateTime.Now;
            _isConverting = true;
            ConvertButtonText = "Stop";
            if (!_timer.IsEnabled) _timer.Start();
        }
    }

    [RelayCommand]
    private void ToggleClaim()
    {
        if (_isClaiming)
        {
            _isClaiming = false;
            ClaimButtonText = "Start Claim";
            ClaimButtonColour = "#32a956";
            CheckTimerState();
            SaveState();
        }
        else
        {
            if (_isRunning || _bankedTime <= TimeSpan.Zero) return;
            if (_isConverting) ToggleConversion();

            _lastTickTime = DateTime.Now;
            _isClaiming = true;
            ClaimButtonText = "Stop Claim";
            ClaimButtonColour = "#e63946";
            if (!_timer.IsEnabled) _timer.Start();
        }
    }

    [RelayCommand]
    private void Reset()
    {
        if (!_isConfirmingReset)
        {
            _isConfirmingReset = true;
            ResetButtonText = "Sure?";
            ResetButtonColour = "#e63946";

            DispatcherTimer.RunOnce(() =>
            {
                if (_isConfirmingReset)
                {
                    RevertResetButton();
                }
            }, TimeSpan.FromSeconds(3));

            return;
        }

        _timer.Stop();
        _isRunning = false;
        _isConverting = false;
        _isClaiming = false;

        _elapsedTime = TimeSpan.Zero;
        _bankedTime = TimeSpan.Zero;
        _initialEarnedTimeForConversion = TimeSpan.Zero;
        ConversionProgress = 0;

        TimeDisplay = "00:00:00";
        BankedTimeDisplay = "00:00:00";

        StartStopButtonText = "Start";
        ConvertButtonText = "Convert";
        ClaimButtonText = "Start Claim";

        StartStopButtonDepressedColour = "#32a956";
        ClaimButtonColour = "#32a956";

        RevertResetButton();
        SaveState();
    }

    [RelayCommand]
    private void OpenBurnModal()
    {
        MinutesToBurnInput = 15;
        ResetBurnConfirmation();
        IsBurnModalOpen = true;
    }

    [RelayCommand]
    private void CloseBurnModal()
    {
        ResetBurnConfirmation();
        IsBurnModalOpen = false;
    }

    [RelayCommand]
    private void ConfirmBurnTime()
    {
        if (!_isConfirmingBurn)
        {
            _isConfirmingBurn = true;
            ConfirmBurnButtonText = "Sure?";
            ConfirmBurnButtonColour = "#d62828";

            DispatcherTimer.RunOnce(() =>
            {
                if (_isConfirmingBurn && IsBurnModalOpen)
                {
                    ResetBurnConfirmation();
                }
            }, TimeSpan.FromSeconds(3));

            return;
        }

        BurnMinutes(MinutesToBurnInput);
        ResetBurnConfirmation();
        IsBurnModalOpen = false;
    }

    #endregion

    #region Helper Methods & Logic

    private void RevertResetButton()
    {
        _isConfirmingReset = false;
        ResetButtonText = "Reset";
        ResetButtonColour = "#9B7EDE";
    }

    private void ResetBurnConfirmation()
    {
        _isConfirmingBurn = false;
        ConfirmBurnButtonText = "Burn Time";
        ConfirmBurnButtonColour = "#e63946";
    }

    public void BurnMinutes(double minutes)
    {
        if (minutes <= 0 || _bankedTime <= TimeSpan.Zero) return;

        TimeSpan timeToBurn = TimeSpan.FromMinutes(minutes);

        if (timeToBurn >= _bankedTime)
        {
            _bankedTime = TimeSpan.Zero;
        }
        else
        {
            _bankedTime -= timeToBurn;
        }

        BankedTimeDisplay = _bankedTime.ToString(@"hh\:mm\:ss");
        SaveState();
    }

    private void CheckTimerState()
    {
        if (!_isRunning && !_isConverting && !_isClaiming)
        {
            _timer.Stop();
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        DateTime now = DateTime.Now;
        TimeSpan delta = now - _lastTickTime;
        _lastTickTime = now;

        if (_isRunning)
        {
            _elapsedTime += delta;
        }

        if (_isConverting)
        {
            if (_elapsedTime > TimeSpan.Zero && _initialEarnedTimeForConversion > TimeSpan.Zero)
            {
                double totalTargetSeconds = TargetConversionMinutes * 60.0;
                double drainRatePerSecond = _initialEarnedTimeForConversion.TotalSeconds / totalTargetSeconds;
                TimeSpan timeToDrain = TimeSpan.FromSeconds(delta.TotalSeconds * drainRatePerSecond);

                if (timeToDrain > _elapsedTime)
                    timeToDrain = _elapsedTime;

                _elapsedTime -= timeToDrain;
                _bankedTime += TimeSpan.FromSeconds(timeToDrain.TotalSeconds * ConversionRatio);

                double remainingRatio = _elapsedTime.TotalSeconds / _initialEarnedTimeForConversion.TotalSeconds;
                ConversionProgress = Math.Clamp((1.0 - remainingRatio) * 100.0, 0.0, 100.0);
            }
            else
            {
                _elapsedTime = TimeSpan.Zero;
                _isConverting = false;
                ConvertButtonText = "Convert";
                ConversionProgress = 100;
                CheckTimerState();
            }
        }

        if (_isClaiming)
        {
            if (_bankedTime > TimeSpan.Zero)
            {
                _bankedTime -= delta;
                if (_bankedTime < TimeSpan.Zero)
                {
                    _bankedTime = TimeSpan.Zero;
                }
            }
            else
            {
                _isClaiming = false;
                ClaimButtonText = "Start Claim";
                ClaimButtonColour = "#32a956";
                CheckTimerState();
            }
        }

        TimeDisplay = _elapsedTime.ToString(@"hh\:mm\:ss");
        BankedTimeDisplay = _bankedTime.ToString(@"hh\:mm\:ss");

        SaveState();
    }

    #endregion

    #region Persistence

    private void SaveState()
    {
        StorageService.SaveData(new AppData
        {
            ElapsedSeconds = _elapsedTime.TotalSeconds,
            BankedSeconds = _bankedTime.TotalSeconds,
            ConversionRatio = ConversionRatio,
            TargetConversionMinutes = TargetConversionMinutes,
            NoteText = NoteText,
            IsFullscreen = IsFullscreen,
            IsTopmost = IsTopmost,
            AppScale = AppScale,
            NotepadFontSize = NotepadFontSize,
            CalendarNotes = _calendarNotes
        });
    }

    private void LoadState()
    {
        AppData data = StorageService.LoadData();
        _elapsedTime = TimeSpan.FromSeconds(data.ElapsedSeconds);
        _bankedTime = TimeSpan.FromSeconds(data.BankedSeconds);
        ConversionRatio = data.ConversionRatio;
        TargetConversionMinutes = data.TargetConversionMinutes;
        NoteText = data.NoteText ?? string.Empty;
        IsFullscreen = data.IsFullscreen;
        IsTopmost = data.IsTopmost;
        AppScale = data.AppScale <= 0 ? 1.0 : data.AppScale;
        NotepadFontSize = data.NotepadFontSize <= 0 ? 14.0 : data.NotepadFontSize;
        _calendarNotes = data.CalendarNotes ?? new Dictionary<string, string>();

        TimeDisplay = _elapsedTime.ToString(@"hh\:mm\:ss");
        BankedTimeDisplay = _bankedTime.ToString(@"hh\:mm\:ss");

        IsCalendarModalOpen = false;
        IsBurnModalOpen = false;
    }

    #endregion
}