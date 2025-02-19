using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;
using GameLauncher.ViewModels;
using GameLauncher.Windows;

namespace GameLauncher.Functions;

public class FriendUtils
{
    public class Friend
    {
        public required string UserName { get; set; }
        public required string DisplayName { get; set; }
        public required string UserId { get; set; }
        public required string Status { get; set; }
        public required int FriendsSinceTimestamp { get; set; }
    }
    
    public class User
    {
        public required string UserName { get; set; }
        public required string DisplayName { get; set; }
        public required string UserId { get; set; }
    }
    
    public class ReceivedFriendRequest
    {
        public required string UserName { get; set; }
        public required string DisplayName { get; set; }
        public required string UserId { get; set; }
        public required int TimeSentTimestamp { get; set; }
    }
    
    public class SentFriendRequest
    {
        public required string UserName { get; set; }
        public required string DisplayName { get; set; }
        public required string UserId { get; set; }
        public required int TimeSentTimestamp { get; set; }
    }
    
    public static ObservableCollection<Friend> GetFriends()
    {
        try
        {
            const string friendsUrl = $"{ServerCommunication.ServerAddress}/friends";
            Console.WriteLine("Getting friends...");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var response = client.GetAsync(friendsUrl).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            json = Uri.UnescapeDataString(json);
            Console.WriteLine(json);
            var friends = JsonSerializer.Deserialize<List<Friend>>(json);

            return new ObservableCollection<Friend>(friends ?? []);
        }
        catch (Exception ex)
        {
            return [];
        }
    }
    
    public static ObservableCollection<ReceivedFriendRequest> GetReceivedFriendRequests()
    {
        try
        {
            const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/requests/received";
            Console.WriteLine("Getting received friend requests...");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var response = client.GetAsync(friendRequestUrl).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            json = Uri.UnescapeDataString(json);
            var friendRequests = JsonSerializer.Deserialize<List<ReceivedFriendRequest>>(json);
            return new ObservableCollection<ReceivedFriendRequest>(friendRequests ?? []);
        } 
        catch (Exception ex) 
        {
            Console.WriteLine("Failed to get received friend requests: " + ex.Message);
            return [];
        }
    }

    public static ObservableCollection<SentFriendRequest> GetSentFriendRequests()
    {
        try
        {
            const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/requests/sent";
            Console.WriteLine("Getting received friend requests...");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var response = client.GetAsync(friendRequestUrl).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            json = Uri.UnescapeDataString(json);
            var friendRequests = JsonSerializer.Deserialize<List<SentFriendRequest>>(json);
            Console.WriteLine("Friend requests: " + friendRequests);
            return new ObservableCollection<SentFriendRequest>(friendRequests ?? []);
        } 
        catch (Exception ex) 
        {
            Console.WriteLine("Failed to get received friend requests: " + ex.Message);
            return [];
        }
    }

    public static ObservableCollection<User> GetBlockedUsers()
    {
        try
        {
            const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/blocked";
            Console.WriteLine("Getting blocked users...");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var response = client.GetAsync(friendRequestUrl).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            json = Uri.UnescapeDataString(json);
            var blockedUsers = JsonSerializer.Deserialize<List<User>>(json);
            return new ObservableCollection<User>(blockedUsers ?? []);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to get blocked users: " + ex.Message);
            return [];
        }
    }
    
    public static async Task LoadFriendStuff(MainViewModel viewmodel) {
        var friends = GetFriends();
        viewmodel.FriendList.Clear();
        foreach (var friend in friends) {
            viewmodel.FriendList.Add(friend);
        }
        var receivedFriendRequests = GetReceivedFriendRequests();
        viewmodel.ReceivedFriendRequests.Clear();
        foreach (var friendRequest in receivedFriendRequests) {
            viewmodel.ReceivedFriendRequests.Add(friendRequest);
        }
        var friendRequestsCount = receivedFriendRequests.Count;
        if (friendRequestsCount > 0) {
            ToastNotification.Show($"You have {friendRequestsCount} friend requests.", -1);
        }
        var sentFriendRequests = GetSentFriendRequests();
        viewmodel.SentFriendRequests.Clear();
        foreach (var friendRequest in sentFriendRequests) {
            viewmodel.SentFriendRequests.Add(friendRequest);
        }
        viewmodel.UpdateFriendRequestText();
        var blockedUsers = GetBlockedUsers();
        viewmodel.BlockedUsers.Clear();
        foreach (var blockedUser in blockedUsers) {
            viewmodel.BlockedUsers.Add(blockedUser);
        }
    }
    
    public static async Task SentFriendRequestToServer(string username, MainViewModel viewModel)
    {
        Console.WriteLine($"AddFriend ViewModel: {viewModel.GetHashCode()}");
        const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/request/add";
        Console.WriteLine(friendRequestUrl);
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
        var content = new StringContent(JsonSerializer.Serialize(new { username }), System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync(friendRequestUrl, content);
        
        if (response.IsSuccessStatusCode)
        {
            var json = response.Content.ReadAsStringAsync().Result;
            json = Uri.UnescapeDataString(json);
            var friendRequest = JsonSerializer.Deserialize<SentFriendRequest>(json);
            Console.WriteLine("Friend requests: " + friendRequest);
            if (friendRequest != null)
            {
                viewModel.SentFriendRequests.Add(friendRequest);
                Console.WriteLine("Friend request sent.");
                ToastNotification.Show("Friend request sent.");
                foreach (var frendRequest in viewModel.SentFriendRequests)
                {
                    Console.WriteLine(frendRequest);
                }
                return;
            }
        }
        
        Console.WriteLine("Failed to send friend request.");
        var errorMessage = RegisterWindow.HtmlStripper.StripHtml(await response.Content.ReadAsStringAsync());
        ToastNotification.Show(errorMessage, 5);
        
    }

    public static async Task AcceptFriendRequest(string userId, MainViewModel viewModel)
    {
        try
        {
            const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/request/accept";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var content = new StringContent(JsonSerializer.Serialize(new { userId }), System.Text.Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(friendRequestUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            if (!response.IsSuccessStatusCode)
            {
                var trimmedResponse = RegisterWindow.HtmlStripper.StripHtml(responseString);
                ToastNotification.Show("Failed to accept friend request: " + trimmedResponse);
                Console.WriteLine("Failed to accept friend request: " + responseString);
                return;     
            }
            RemoveReceivedFriendRequest(userId, viewModel);
            var json = response.Content.ReadAsStringAsync().Result;
            json = Uri.UnescapeDataString(json);
            var friend = JsonSerializer.Deserialize<Friend>(json);
            Console.WriteLine("Friend requests: " + friend);
            if (friend != null)
            {
                viewModel.FriendList.Add(friend);
                Console.WriteLine("Friend request sent.");
                ToastNotification.Show("Friend request sent.");
            } else {
                Console.WriteLine("Failed to accept friend request.");
                var errorMessage = RegisterWindow.HtmlStripper.StripHtml(await response.Content.ReadAsStringAsync());
                ToastNotification.Show(errorMessage);
            }
        } 
        catch (Exception ex) 
        {
            ToastNotification.Show("Failed to accept friend request: " + ex.Message);
            Console.WriteLine("Failed to accept friend request: " + ex.Message);
        }
    }
    
    private static void RemoveReceivedFriendRequest(string userId, MainViewModel viewModel)
    {
        var copyReceivedFriendRequests = viewModel.ReceivedFriendRequests;
        viewModel.ReceivedFriendRequests.Clear();
        foreach (var friendRequest in copyReceivedFriendRequests)
        {
            if (friendRequest.UserId == userId) continue;
            viewModel.ReceivedFriendRequests.Add(friendRequest);
        }
        viewModel.UpdateFriendRequestText();
    }

    public static async Task CancelFriendRequest(string userId, MainViewModel viewModel)
    {
        try
        {
            Console.WriteLine($"CancelFriendRequest ViewModel: {viewModel.GetHashCode()}");

            const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/request/cancel";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var content = new StringContent(JsonSerializer.Serialize(new { userId }), System.Text.Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(friendRequestUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            if (!response.IsSuccessStatusCode)
            {
                var trimmedResponse = RegisterWindow.HtmlStripper.StripHtml(responseString);
                ToastNotification.Show("Failed to cancel friend request: " + trimmedResponse);
                Console.WriteLine("Failed to cancel friend request: " + responseString);
            }
            else
            {
                ToastNotification.Show("Friend request cancelled.");
                var copySentFriendRequests = viewModel.SentFriendRequests;
                viewModel.SentFriendRequests.Clear();
                foreach (var friendRequest in copySentFriendRequests)
                {
                    if (friendRequest.UserId == userId) continue;
                    viewModel.SentFriendRequests.Add(friendRequest);
                }
            }
        } 
        catch (Exception ex) 
        {
            ToastNotification.Show("Failed to cancel friend request: " + ex.Message);
            Console.WriteLine("Failed to cancel friend request: " + ex.Message);
        }
    }

    public static async Task DeclineFriendRequest(string userId, MainViewModel viewModel)
    {
        try
        {
            const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/request/decline";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var content = new StringContent(JsonSerializer.Serialize(new { userId }), System.Text.Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(friendRequestUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            if (!response.IsSuccessStatusCode)
            {
                var trimmedResponse = Windows.RegisterWindow.HtmlStripper.StripHtml(responseString);
                ToastNotification.Show("Failed to decline friend request: " + trimmedResponse);
                Console.WriteLine("Failed to decline friend request: " + responseString);
            }
            else
            {
                RemoveReceivedFriendRequest(userId, viewModel);
                ToastNotification.Show("Friend request declined.");
            }
        } 
        catch (Exception ex) 
        {
            ToastNotification.Show("Failed to decline friend request: " + ex.Message);
            Console.WriteLine("Failed to decline friend request: " + ex.Message);
        }
    }

    public static async Task BlockUser(string userId, MainViewModel viewModel)
    {
        try
        {
            const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/block";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var content = new StringContent(JsonSerializer.Serialize(new { userId }), System.Text.Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(friendRequestUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            if (!response.IsSuccessStatusCode)
            {
                var trimmedResponse = RegisterWindow.HtmlStripper.StripHtml(responseString);
                ToastNotification.Show("Failed to block user: " + trimmedResponse);
                Console.WriteLine("Failed to block user: " + responseString);
            }
            else
            {
                var json = response.Content.ReadAsStringAsync().Result;
                json = Uri.UnescapeDataString(json);
                var user = JsonSerializer.Deserialize<User>(json);
                if (user == null)
                {
                    ToastNotification.Show("Failed Updating View, restart please.");
                    return;
                }
                RemoveReceivedFriendRequest(userId, viewModel);
                viewModel.BlockedUsers.Add(user);
            }
        } 
        catch (Exception ex) 
        {
            ToastNotification.Show("Failed to block user: " + ex.Message);
            Console.WriteLine("Failed to block user: " + ex.Message);
        }
    }
}