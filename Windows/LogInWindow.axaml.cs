using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GameLauncher.Functions;
using GameLauncher.ViewModels;

namespace GameLauncher.Windows;

public partial class LogInWindow : Window
{
    private readonly MainViewModel _viewModel;

    public LogInWindow()
    {
        InitializeComponent();
    }
    public LogInWindow(MainViewModel viewModel)
        :this()
    {
        _viewModel = viewModel;
    }

    private async void LogIn(object? sender, RoutedEventArgs routedEventArgs)
    {
        try
        {
            await GetCookie();
            await UserUtils.CreateUser(_viewModel);
            Close();
        } catch (Exception ex)
        {
            ToastNotification.Show("Failed to log in: " + ex.Message);
            Console.WriteLine("Failed to log in: " + ex.Message);
        }
        
        
        
    }

    private async Task GetCookie()
    {
        try
        {
            const string loginUrl = $"{ServerCommunication.ServerAddress}/login/launcher";

            Console.WriteLine(loginUrl);
            var username = this.FindControl<TextBox>("Username")?.Text;
            var password = this.FindControl<TextBox>("Password")?.Text;
            if (username == null || password == null)
            {   
                ToastNotification.Show("Please enter a username and password.");
                return;
            }
            var loginData = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password }
            };
            Console.WriteLine("Logging in...");
            Console.WriteLine("Username: " + username);
            Console.WriteLine("Password: " + password);

            var client = new HttpClient();
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
            var response = client.PostAsync(loginUrl, content).Result;

            var sessionCookies = response.Headers.GetValues("Set-Cookie").ToArray();
            if (sessionCookies.Length <= 0) return;
            var firstCookie = sessionCookies[0];

            var cookieValue = firstCookie.Split(';')[0];

            if (!cookieValue.Contains("session")) return;

            var encryptedCookie = EncryptionUtils.EncryptCookie(cookieValue);
            var cookieFile = $"{Program.BasePath}/session.cookie";
            await System.IO.File.WriteAllTextAsync(cookieFile, (string?)encryptedCookie);
            Console.WriteLine("Logged in.");
        }
        catch (Exception ex)
        {
            ToastNotification.Show("Failed to log in: " + ex.Message);
            Console.WriteLine("Failed to log in: " + ex.Message);
        }
    }
}