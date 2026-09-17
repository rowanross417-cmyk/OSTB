using System.ComponentModel;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Threading;
using OSTB.ViewModels;

namespace OSTB.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeWindowHandlers();
    }

    private void MainWindow_DataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += Vm_PropertyChanged;

            // apply initial state
            Topmost = vm.IsTopmost;
            WindowState = vm.IsFullscreen ? WindowState.FullScreen : WindowState.Normal;
        }
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainViewModel vm)
        {
            if (e.PropertyName == nameof(vm.IsTopmost))
            {
                Topmost = vm.IsTopmost;
            }
            else if (e.PropertyName == nameof(vm.IsFullscreen))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    WindowState = vm.IsFullscreen ? WindowState.FullScreen : WindowState.Normal;
                });
            }
        }
    }

    // Subscribe when constructed; handler will run when App sets DataContext
    public void InitializeWindowHandlers()
    {
        DataContextChanged += MainWindow_DataContextChanged;
    }
}