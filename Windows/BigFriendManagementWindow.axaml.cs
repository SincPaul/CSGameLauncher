using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GameLauncher.ViewModels;

namespace GameLauncher.Windows;

public partial class BigFriendManagementWindow : Window
{
    public BigFriendManagementWindow()
    {
        InitializeComponent();
    }

    public BigFriendManagementWindow(MainViewModel mainViewModel)
        : this()
    {
        this.Resources["MainViewModel"] = mainViewModel;
        DataContext = mainViewModel;
    }

    private void ManageFriend(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ShowUser(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Showing user.");
    }
}