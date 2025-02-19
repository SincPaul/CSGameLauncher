using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GameLauncher.ViewModels;
using GameLauncher.Windows;

namespace GameLauncher.Views;

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
        Console.WriteLine("Opening friend config.");
    }

    private void LogIn(object? sender, RoutedEventArgs e)
    {
        var ownerWindow = (Window) this.VisualRoot!;
        var mainViewModel = (DataContext as MainViewModel);
        if (mainViewModel == null)
        {
            ToastNotification.Show("Failed to open log in window.");
            return;
        }
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


    

    private void OpenAddFriendMenu(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Opening add friend menu.");
        (DataContext as MainViewModel)?.ToggleFriendAddMenu(true);
    }

    private void CloseAddFriendMenu(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Opening add friend menu.");
        (DataContext as MainViewModel)?.ToggleFriendAddMenu(false);
    }

    private void OpenFriendRequestManagement(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainViewModel;
        if (viewModel == null)
        {
            ToastNotification.Show("Failed to open friend request management window.");
            return;
        }
        var bigFriendManagementWindow = new BigFriendManagementWindow(viewModel);
        bigFriendManagementWindow.Show();
    }
}
