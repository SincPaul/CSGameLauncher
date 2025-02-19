using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using GameLauncher.ViewModels;

namespace GameLauncher.Functions;

public class UserUtils
{
    private static readonly string CookieFile = $"{Program.BasePath}/session.cookie";
    public static string UserCookie = string.Empty;
    public static async Task CreateUser(MainViewModel viewModel)
    {
        if (!File.Exists(CookieFile))
        {
            Console.WriteLine("No cookie file found");
            return;
        }
        
        var encryptedCookie = File.ReadAllText(CookieFile);
        var decryptedCookie = EncryptionUtils.DecryptCookie(encryptedCookie);
        UserCookie = decryptedCookie;
        
        Console.WriteLine("Creating user...");
        Console.WriteLine("Cookie: " + decryptedCookie);
        
        const string userUrl = $"{ServerCommunication.ServerAddress}/ownuser";
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Cookie", decryptedCookie);
        var response = client.GetAsync(userUrl).Result;
        var responseContent = response.Content.ReadAsStringAsync().Result;
        Console.WriteLine("Response: " + responseContent);
        Console.WriteLine("Response" + response);
        var mainUser = JsonSerializer.Deserialize<User>(responseContent);
        if (mainUser == null)
        {
            Console.WriteLine("Failed to create user");
            return;
        }
        Console.WriteLine("User created.");
        Console.WriteLine("User: " + mainUser.username);
        viewModel.SetLoggedIn(true);
        LoadFriends(decryptedCookie);
    }
    
    private static void LoadFriends(string decryptedCookie)
    {
        try
        {
            const string friendsWsUrl = $"{ServerCommunication.WebsocketAddress}/friends";
            var webSocket = new ClientWebSocket();
            webSocket.Options.SetRequestHeader("Cookie", decryptedCookie);
            webSocket.ConnectAsync(new Uri(friendsWsUrl), default).Wait();
            Console.WriteLine("Connected to friends websocket");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to connect to friends websocket: " + ex.Message);
        }
    }

    public static void LogOut(MainViewModel viewModel)
    {
        if (!File.Exists(CookieFile))
        {
            Console.WriteLine("No cookie file found");
            return;
        }
        
        var encryptedCookie = File.ReadAllText(CookieFile);
        var decryptedCookie = EncryptionUtils.DecryptCookie(encryptedCookie);
        
        Console.WriteLine("Logging out...");
        const string logoutUrl = $"{ServerCommunication.ServerAddress}/logout/launcher";
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Cookie", decryptedCookie);
        var response = client.GetAsync(logoutUrl).Result;
        var responseContent = response.Content.ReadAsStringAsync().Result;
        Console.WriteLine("Response: " + responseContent);
        Console.WriteLine("Response" + response);
        File.Delete(CookieFile);
        viewModel.SetLoggedIn(false);
    }

    public class User
    {
        public string uuid { get; set; }
        public string username { get; set; }
        public string displayname { get; set; }
        public string human_displayname { get; set; }
        public string human_username { get; set; }
        public string role { get; set; }
        public string email { get; set; }
        BannedStatus banned { get; set; }
    }
        
    private class BannedStatus
    {
        public string reason { get; set; }
        public long banned_until { get; set; }
        public int status { get; set; }
    }
}