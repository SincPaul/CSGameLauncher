using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace GameLauncher;

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
        throw new NotImplementedException();
    }

    private void Register(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}