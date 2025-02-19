using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GameLauncher.Functions;
using GameLauncher.ViewModels;

namespace GameLauncher.Windows;

public partial class RegisterWindow : Window
{
    private readonly MainViewModel _viewModel;
    public RegisterWindow()
    {
        InitializeComponent();
    }
    public RegisterWindow(MainViewModel viewModel) 
        : this()
    {
        _viewModel = viewModel;
    }

    private async void Register(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!PasswordMatches()) return;
            SendToServer();
            ToastNotification.Show("Click on the link in the email to activate your account.", -1);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("Failed to register: " + ex.Message);
        }
    }

    private bool PasswordMatches()
    {
        var password = Password.Text;
        var repeatPassword = PasswordRepeat.Text;
        if (password == repeatPassword) return true;
        Console.WriteLine("Passwords do not match.");
        return false;
    }

    private async void SendToServer()
    {
        try
        {
            const string registerUrl = $"{ServerCommunication.ServerAddress}/register/launcher";
            Console.WriteLine(registerUrl);
            var username = Username.Text;
            var password = Password.Text;
            var email = Email.Text;
            var registerData = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password },
                { "email", email }
            };
            Console.WriteLine("Registering...");
            Console.WriteLine("Username: " + username);
            Console.WriteLine("Password: " + password);
            Console.WriteLine("Email: " + email);

            var client = new HttpClient();
            var content = new StringContent(JsonSerializer.Serialize(registerData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(registerUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                var trimmedResponse = HtmlStripper.StripHtml(responseString);

                ToastNotification.Show("Failed to register: " + trimmedResponse);
                Console.WriteLine("Failed to register: " + responseString);
                return;
            }
            Console.WriteLine(responseString);
        }
        catch (Exception e)
        {
            ToastNotification.Show(e.Message);
            Console.WriteLine("Failed to register: " + e.Message);
        }
    }

    public abstract partial class HtmlStripper
    {
        [GeneratedRegex("</?(p|br)\\s*/?>", RegexOptions.IgnoreCase)]
        private static partial Regex NewlineRegex();

        [GeneratedRegex("<.*?>")]
        private static partial Regex TagRegex();

        public static string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            input = NewlineRegex().Replace(input, "\n");

            input = TagRegex().Replace(input, string.Empty);

            return MyRegex().Replace(input, "\n").Trim();
        }

        [GeneratedRegex(@"\n+")]
        private static partial Regex MyRegex();
    }
}