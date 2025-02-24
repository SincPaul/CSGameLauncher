using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GameLauncher.Windows;

public partial class ToastNotification : Window
{
    private readonly TextBlock? _messageText;
    public ToastNotification()
    {
        InitializeComponent();
        _messageText = this.FindControl<TextBlock>("TextBlock");
    }
    
    public static async void Show(string? message, int timeout = 5)
    {
        
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                Console.WriteLine("Showing toast notification.");
                Console.WriteLine(message);
                if (message == null) return;
                var window = new ToastNotification();
                if (window._messageText != null) window._messageText.Text = message;
                window.Show();

                if (timeout == -1) return;
                timeout *= 1000;
                await Task.Delay(timeout);
                window.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to show toast notification: " + ex.Message);
            }
        });
    }
    
    private void Close(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}