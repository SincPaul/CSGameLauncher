using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
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
            var friends = JsonSerializer.Deserialize<List<Friend>>(json) ?? [];
            Console.WriteLine("Friends COunt: " + friends.Count);
            return new ObservableCollection<Friend>(friends);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to get friends: " + ex.Message);
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
            var friendRequests = JsonSerializer.Deserialize<List<ReceivedFriendRequest>>(json) ?? [];
            Console.WriteLine("Received Friend requests Count: " + friendRequests.Count);
            return new ObservableCollection<ReceivedFriendRequest>(friendRequests);
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
            Console.WriteLine("Getting sent friend requests...");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var response = client.GetAsync(friendRequestUrl).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            json = Uri.UnescapeDataString(json);
            var friendRequests = JsonSerializer.Deserialize<List<SentFriendRequest>>(json) ?? [];
            Console.WriteLine("Sent Friend requests count: " + friendRequests.Count);
            return new ObservableCollection<SentFriendRequest>(friendRequests);
        } 
        catch (Exception ex) 
        {
            Console.WriteLine("Failed to get sent friend requests: " + ex.Message);
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
            var blockedUsers = JsonSerializer.Deserialize<List<User>>(json) ?? [];
            Console.WriteLine("Blocked users count: " + blockedUsers.Count);
            return new ObservableCollection<User>(blockedUsers);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to get blocked users: " + ex.Message);
            return [];
        }
    }
    
    public static async Task LoadFriendStuff(MainViewModel viewmodel) {
        var friends = GetFriends();
        Console.WriteLine("Friends: " + friends);
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
        viewmodel.UpdateFriendViews();
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
        ToastNotification.Show(errorMessage);
        
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
                Console.WriteLine("Accepted Friend Request.");
                //ToastNotification.Show("ACcepted Friend Request.");
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
                var trimmedResponse = RegisterWindow.HtmlStripper.StripHtml(responseString);
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
            const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/request/block";
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
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    ToastNotification.Show("Failed to block user: " + trimmedResponse);
                    Console.WriteLine("Failed to block user: " + responseString);
                });
            }
            else
            {
                try
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    json = Uri.UnescapeDataString(json);
                    var user = JsonSerializer.Deserialize<User>(json);
                    try
                    {
                        RemoveReceivedFriendRequest(userId, viewModel);
                    } catch
                    {
                        //ignored
                    }
                    try
                    {
                        viewModel.SentFriendRequests.Remove(viewModel.SentFriendRequests.First(request => request.UserId == userId));
                    }
                    catch
                    {
                        //ignored
                    }
                    
                    try
                    {
                        viewModel.FriendList.Remove(viewModel.FriendList.First(friend => friend.UserId == userId));
                    }
                    catch
                    {
                        // ignored
                    }

                    if (user == null)
                    {
                        ToastNotification.Show("Failed Updating View, restart please.");
                        Console.WriteLine("Failed Updating View, restart please.");
                        return;
                    }
                    viewModel.BlockedUsers.Add(user);
                } 
                catch (Exception ex)
                {
                    ToastNotification.Show("Failed Updating View, restart please. " + ex.Message);
                    Console.WriteLine("Failed Updating View, restart please. " + ex.Message);
                }
            }
        } 
        catch (Exception ex) 
        {
            ToastNotification.Show("Failed to block user: " + ex.Message);
            Console.WriteLine("Failed to block user: " + ex.Message);
        }
    }

    public static async Task RemoveFriend(string userId, MainViewModel viewModel)
    {
        try
        {
            Console.WriteLine($"RemoveFriend ViewModel: {viewModel.GetHashCode()}");
            const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/request/remove";
            Console.WriteLine(friendRequestUrl);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var content = new StringContent(JsonSerializer.Serialize(new { userId }), System.Text.Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(friendRequestUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = RegisterWindow.HtmlStripper.StripHtml(await response.Content.ReadAsStringAsync());
                ToastNotification.Show("Failed to remove friend.\n" + errorMessage);
                return;
            }

            viewModel.FriendList.Remove(viewModel.FriendList.First(user => user.UserId == userId));
        }
        catch (Exception ex)
        {
            ToastNotification.Show("Failed to remove friend: " + ex.Message);
            Console.WriteLine("Failed to remove friend: " + ex.Message);
        }
    }

    public static async Task UnblockUser(string userId, MainViewModel viewModel)
    {   
        try
        {
            Console.WriteLine($"Unblock ViewModel: {viewModel.GetHashCode()}");
            const string friendRequestUrl = $"{ServerCommunication.ServerAddress}/friends/request/unblock";
            Console.WriteLine(friendRequestUrl);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", UserUtils.UserCookie);
            var content = new StringContent(JsonSerializer.Serialize(new { userId }), System.Text.Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(friendRequestUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = RegisterWindow.HtmlStripper.StripHtml(await response.Content.ReadAsStringAsync());
                ToastNotification.Show("Failed to unblock user.\n" + errorMessage);
                return;
            }

            viewModel.BlockedUsers.Remove(viewModel.BlockedUsers.First(user => user.UserId == userId));
        }
        catch (Exception ex)
        {
            ToastNotification.Show("Failed to unblock user: " + ex.Message);
            Console.WriteLine("Failed to unblock user " + ex.Message);
        }
    }

    public static void HandleFriendWsMessage(string message, MainViewModel viewModel)
    {
        var jsonDocument = JsonDocument.Parse(message);
        var type = jsonDocument.RootElement.GetProperty("type").GetString();
    
        var handlers = new Dictionary<string, Action<JsonDocument, MainViewModel>>
        {
            { "friend_status", HandleFriendStatusMessage },
            { "friend_request_accepted", HandleAcceptedFriendRequest },
            { "friend_request_declined", HandleDeclinedFriendRequest },
            { "friend_request_canceled", HandleCanceledFriendRequest },
            { "friend_request_sent", HandleReceivedFriendRequest },
            { "user_blocked", HandleBlockedUser },
            { "friend_removed", HandleFriendRemoved }

        };
    
        if (handlers.TryGetValue(type, out var handler))
        {
            handler(jsonDocument, viewModel);
            viewModel.UpdateFriendViews();
        }
        else
        {
            Console.WriteLine("Unknown message type: " + type);
        }
    }
    

    private static void HandleFriendStatusMessage(JsonDocument jsonDocument, MainViewModel viewModel)
    {
        var uuid = jsonDocument.RootElement.GetProperty("uuid").GetString();
        var status = jsonDocument.RootElement.GetProperty("status").GetString();
        Console.WriteLine($"Friend {uuid} is now {status}");
        var friend = viewModel.FriendList.FirstOrDefault(friend => friend.UserId == uuid);
        if (friend == null || status == null) return;
        friend.Status = status;
        viewModel.ChangeFriendStatus(friend);
    }
    
    private static async void HandleAcceptedFriendRequest(JsonDocument jsonDocument, MainViewModel viewModel)
    {
        try
        {
            var user = jsonDocument.RootElement.GetProperty("user");
            var friend = JsonSerializer.Deserialize<Friend>(user.GetRawText());
            if (friend == null) return;
            viewModel.FriendList.Add(friend);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ToastNotification.Show($"{friend.DisplayName} accepted your friend request.");
                return Task.CompletedTask;
            });
            viewModel.SentFriendRequests.Remove(viewModel.SentFriendRequests.First(request => request.UserId == friend.UserId));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to handle accepted friend request: " + ex.Message);
        }
    }

    private static void HandleDeclinedFriendRequest(JsonDocument jsonDocument, MainViewModel viewModel)
    {
        try
        {
            var userId = jsonDocument.RootElement.GetProperty("userId").GetString();
            viewModel.SentFriendRequests.Remove(viewModel.SentFriendRequests.First(request => request.UserId == userId));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to handle declined friend request: " + ex.Message);
        }
    }

    private static void HandleCanceledFriendRequest(JsonDocument jsonDocument, MainViewModel viewModel)
    {
        try
        {
            var userId = jsonDocument.RootElement.GetProperty("userId").GetString();
            viewModel.ReceivedFriendRequests.Remove(viewModel.ReceivedFriendRequests.First(request => request.UserId == userId));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to handle canceled friend request: " + ex.Message);
        }
    }

    private static async void HandleReceivedFriendRequest(JsonDocument jsonDocument, MainViewModel viewModel)
    {
        try
        {
            var user = jsonDocument.RootElement.GetProperty("user");
            var friendRequest = JsonSerializer.Deserialize<ReceivedFriendRequest>(user.GetRawText());
            if (friendRequest == null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ToastNotification.Show("Received friend request, but failed to display, restart please:");
                });
                Console.WriteLine("Failed to handle received friend request");
                return;
            }
            viewModel.ReceivedFriendRequests.Add(friendRequest);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ToastNotification.Show($"{friendRequest.DisplayName} sent you a friend request.");
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ToastNotification.Show("Received friend request, but failed to display, restart please: " +
                                       ex.Message);
            });
            Console.WriteLine("Failed to handle received friend request: " + ex.Message);
        }
    }
    
    private static async void HandleBlockedUser(JsonDocument jsonDocument, MainViewModel viewModel)
    {
        try
        {
            var userId = jsonDocument.RootElement.GetProperty("userId").GetString();
            if (userId == null) return;
            var user = viewModel.FriendList.FirstOrDefault(friend => friend.UserId == userId);
            if (user == null) return;
            viewModel.FriendList.Remove(user);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ToastNotification.Show($"{user.DisplayName} blocked you.");
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ToastNotification.Show("Blocked user, but failed to display, restart please: " + ex.Message);
            });
            Console.WriteLine("Failed to handle blocked user: " + ex.Message);
        }
    }
    
    private static async void HandleFriendRemoved(JsonDocument jsonDocument, MainViewModel viewModel)
    {
        try
        {
            var userId = jsonDocument.RootElement.GetProperty("userId").GetString();
            if (userId == null) return;
            var user = viewModel.FriendList.FirstOrDefault(friend => friend.UserId == userId);
            if (user == null) return;
            viewModel.FriendList.Remove(user);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ToastNotification.Show($"{user.DisplayName} removed you as a friend.");
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ToastNotification.Show("Friend removed, but failed to display, restart please: " + ex.Message);
            });
            Console.WriteLine("Failed to handle friend removed: " + ex.Message);
        }
    }
}
