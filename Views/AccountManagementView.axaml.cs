using Avalonia.Controls;
using Avalonia.Interactivity;
using GameLauncher.Functions;
using GameLauncher.ViewModels;
using GameLauncher.Windows;

namespace GameLauncher.Views;

public partial class AccountManagementView : UserControl
{
    public AccountManagementView()
    {
        InitializeComponent();
    }
    
    private void OpenAccountMenu(object? sender, RoutedEventArgs e)
    {
        AccountManagementPopup.IsOpen = true;
    }

    private void CloseAccountMenu(object? sender, RoutedEventArgs e)
    {
        AccountManagementPopup.IsOpen = false;
    }

    private void LogIn(object? sender, RoutedEventArgs e)
    {
        var ownerWindow = (Window) this.VisualRoot!;
        var mainViewModel = (DataContext as MainViewModel);
        if (mainViewModel == null) return;
        var window = new LogInWindow(mainViewModel);
        window.ShowDialog(ownerWindow);
        // throw new NotImplementedException();
    }

    private void Register(object? sender, RoutedEventArgs e)
    {
        var ownerWindow = (Window) this.VisualRoot!;
        var mainViewModel = (DataContext as MainViewModel);
        if (mainViewModel == null)
        {   
            ToastNotification.Show("Failed to open register window.");
            return;
        }
        var window = new RegisterWindow(mainViewModel);
        window.ShowDialog(ownerWindow);
    }

    private void LogOut(object? sender, RoutedEventArgs e)
    {
        var mainViewModel = (DataContext as MainViewModel);
        if (mainViewModel == null)
        {
            ToastNotification.Show("Failed to log out.");
            return;
        }
        UserUtils.LogOut(mainViewModel);
    }
}