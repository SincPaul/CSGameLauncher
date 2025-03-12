using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GameLauncher.Functions;
using GameLauncher.ViewModels;
using GameLauncher.Windows;
using Tmds.DBus.Protocol;

namespace GameLauncher.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    private void OpenChat(object? sender, RoutedEventArgs e)
    {
        ChatPopup.IsOpen = true;
    }

    private void LogIn(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("not here");
    }

    private void Register(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("not here");
    }
    
    private void CloseChat(object? sender, RoutedEventArgs e)
    {
        ChatPopup.IsOpen = false;
    }


    private void OpenChatWithFriend(object? sender, RoutedEventArgs e)
    {
        
        Console.WriteLine("Opening chat with friend. ");
        var viewModel = (DataContext as MainViewModel);
        if (viewModel == null)
        {
            Console.WriteLine("Failed to open chat with friend.");
            ToastNotification.Show("Failed to open chat with friend.");
            return;
        }
        var friend = (sender as Button)?.Tag as FriendUtils.Friend;
        if (friend == null)
        {
            Console.WriteLine("Failed to open chat with friend.");
            ToastNotification.Show("Failed to open chat with friend.");
            return;
        }
        viewModel.LoadChatMessages(friend, 0);
        viewModel.UpdateSelectedChat(friend);
        
    }

    private void SendChatMessage(object? sender, RoutedEventArgs e)
    {
        var viewModel = (DataContext as MainViewModel);
        if (viewModel == null)
        {
            Console.WriteLine("Failed to send chat message.");
            ToastNotification.Show("Failed to send chat message.");
            return;
        }
        
        var receiver = viewModel.SelectedChatFriend[0];
    
        var receiverId = receiver.UserId?.ToString();
        if (receiverId == null)
        {
            Console.WriteLine("Failed to send chat message.");
            ToastNotification.Show("Failed to send chat message.");
            return;
        }
        
        var senderUser = viewModel.MainUser;

        var senderId = senderUser.uuid?.ToString();
        
        if (senderId == null)
        {
            Console.WriteLine("Failed to send chat message.");
            ToastNotification.Show("Failed to send chat message.");
            return;
        }
        
        var sendText = viewModel.CurrentTextMessage.Trim();
        if (sendText == string.Empty)
        {
            Console.WriteLine("Input a chat message before sending.");
            ToastNotification.Show("Input a chat message before sending.");
            return;
        }

        var message = new ChatUtils.ChatMessage
        {
            Message = sendText,
            SenderId = senderId,
            ReceiverId = receiverId,
            TimeSentTimestamp = DateTime.Now
        };
        
        _ = viewModel.SendChatMessage(message);
        viewModel.CurrentTextMessage = string.Empty;
    }
}