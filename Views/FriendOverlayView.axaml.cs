using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GameLauncher.ViewModels;

namespace GameLauncher;

public partial class FriendOverlayView : UserControl
{
    public FriendOverlayView()
    {
        InitializeComponent();
    }
    
    private void OpenFriendMenu(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Opening friend menu.");
        (DataContext as MainViewModel)?.ToggleFriendMenu(true);
        FriendPopup.IsOpen = true;
    }
    
    private void CloseFriendMenu(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Closing friend menu.");
        (DataContext as MainViewModel)?.ToggleFriendMenu(false);
        FriendPopup.IsOpen = false;
    }

    private void OpenFriendConfig(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void LogIn(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
        //var OwnerWindow = (Window) this.VisualRoot;
        //var loginWindow = new LoginWindow();
        //loginWindow.ShowDialog(OwnerWindow);
    }

    private void Register(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
        

}