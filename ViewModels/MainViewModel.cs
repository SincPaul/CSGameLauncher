using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameLauncher.Functions;
using GameLauncher.Windows;

namespace GameLauncher.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _windowtitle = "Stein Game Launcher";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PlayButtonText), nameof(VisibilityStatus))]
        private bool _isloading = true;

        [ObservableProperty]
        private string _loadingtext = "Loading Games...";

        public WebSocketClient friendClient;

        public string PlayButtonText { get; set; } = "Loading Games";

        public bool VisibilityStatus => Isloading;

        public ServerCommunication.GameStats? GameTag { get; set; }

        private ObservableCollection<ServerCommunication.GameStats> GameList { get; } = [];
        public ObservableCollection<ServerCommunication.GameStats> OfflineGameList { get; } = [];
        public ObservableCollection<ServerCommunication.GameStats> OnlineGameList { get; } = [];
        
        public ObservableCollection<FriendUtils.Friend> FriendList { get; set; } = [];

        public ObservableCollection<FriendUtils.Friend> SelectedChatFriend { get; set; } = [];
        
        public ObservableCollection<FriendUtils.ReceivedFriendRequest> ReceivedFriendRequests { get; set; } = [];
        public ObservableCollection<FriendUtils.SentFriendRequest> SentFriendRequests { get; set; } = [];
        
        public ObservableCollection<FriendUtils.User> BlockedUsers { get; set; } = [];
        
        public Dictionary<string, List<ChatUtils.ChatMessage>> ChatMessages { get; set; } = new();
        public string FriendRequestsAmountWithText { get; set; } = "Friend Requests (0)";
        public string LastPatch { get; set; } = string.Empty;

        public string SelectedGame { get; set; } = string.Empty;
        
        public string DownloadStatus { get; set; } = string.Empty;
        
        public bool IsDownloading { get; set; }
        
        public bool IsServerError { get; set; }
        
        public string ServerErrorText { get; set; } = "Couldnt connect to server";
        
        public bool IsServerConnected { get; set; }
        public bool IsServerConnectedAndUserLoggedIn { get; set; }
        public bool IsServerConnectedAndUserNotLoggedIn { get; set; }
        
        public bool ButtonEnabled { get; set; }
        public bool IsFriendMenuVisible { get; set; }
        public bool IsLoggedIn { get; set; }
        public bool IsAddingFriend { get; set; }

        public bool NoChatSelected { get; set; } = true;
        
        public string CurrentTextMessage { get; set; } = string.Empty;

        public bool IsLoadingMessages { get; set; }
        
        public WebSocketClient chatClient { get; set; }
        
        public UserUtils.User MainUser { get; set; }
        
        
        public void ChangeLoadingStatus(bool status)
        {
            Isloading = status;
            OnPropertyChanged(nameof(PlayButtonText));
            OnPropertyChanged(nameof(VisibilityStatus));
            OnPropertyChanged(nameof(SelectedGame));
        }
        
        [RelayCommand]
        private void SwapMainGame(ServerCommunication.GameStats? game)
        {
            if (game == null) return;
            SelectedGame = game.Name;
            PlayButtonText = game.Status;
            var uploadedAtDateTime = DateTimeOffset.FromUnixTimeSeconds(game.UploadedAt).DateTime;

            var timeSinceUpload = DateTime.UtcNow - uploadedAtDateTime;
            
            Console.WriteLine(timeSinceUpload);
            Console.WriteLine(LastPatch);
            GameTag = game;
            if (game.ForceNewVersion)
            {
                if (IsServerConnected)
                {
                    ButtonEnabled = true;
                } else
                {
                    ButtonEnabled = false;
                }
            } else
            {
                ButtonEnabled = true;
            }
            OnPropertyChanged(nameof(SelectedGame));
            OnPropertyChanged(nameof(PlayButtonText));
            OnPropertyChanged(nameof(LastPatch));
            OnPropertyChanged(nameof(GameTag));
            OnPropertyChanged(nameof(ButtonEnabled));
        }
        
        public void ShowGames(ObservableCollection<ServerCommunication.GameStats> gameList)
        {
            GameList.Clear();
            OnlineGameList.Clear();
            OfflineGameList.Clear();
            foreach (var game in gameList)
            {
                Console.WriteLine(game.Name);
                GameList.Add(game);
                Console.WriteLine("GAME: " + game.Name + "FORCE: " + game.ForceNewVersion);
                if (game.ForceNewVersion)
                {
                    OnlineGameList.Add(game);
                } else
                {
                    OfflineGameList.Add(game);
                }
            }

            if (GameList.Count <= 0) return;
            SelectedGame = GameList[0].Name;
            PlayButtonText = GameList[0].Status;
            OnPropertyChanged(nameof(SelectedGame));
        }
        
        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            Console.WriteLine(timeSpan);
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays >= 2 ? "s" : "")}";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours >= 2 ? "s" : "")}";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes >= 2 ? "s" : "")}";
            return $"{(int)timeSpan.TotalSeconds} second{(timeSpan.TotalSeconds >= 2 ? "s" : "")}";
        }
        
        public void UpdateDownloadProgress(long lastSecondBytesReceived, long totalBytesReceived, long totalBytesToReceive)
        {
            long downloadSpeed = 0;
            var eta = TimeSpan.Zero;
            if (lastSecondBytesReceived != 0)
            {
                downloadSpeed = lastSecondBytesReceived / 1024;
                eta = TimeSpan.FromSeconds((totalBytesToReceive - totalBytesReceived) / lastSecondBytesReceived);
            }
            var downloadSpeedText = downloadSpeed > 1024 ? $"{downloadSpeed / 1024} MB/s" : $"{downloadSpeed} KB/s";
            var downloadProgressPercent = (double)totalBytesReceived / totalBytesToReceive * 100;
            var downloadProgressReceived = totalBytesReceived >= 1024 * 1024 * 1024 ? $"{totalBytesReceived / (1024 * 1024 * 1024.0):0.00} GB"
                    : totalBytesReceived >= 1024 * 1024 ? $"{totalBytesReceived / (1024 * 1024.0):0.00} MB" 
                    : totalBytesReceived >= 1024 ? $"{totalBytesReceived / 1024.0:0.00} KB" : $"{totalBytesReceived} Bytes";
                var downloadProgressTotal = totalBytesToReceive >= 1024 * 1024 * 1024 ? $"{totalBytesToReceive / (1024 * 1024 * 1024.0):0.00} GB" 
                    : totalBytesToReceive >= 1024 * 1024 ? $"{totalBytesToReceive / (1024 * 1024.0):0.00} MB" 
                    : totalBytesToReceive >= 1024 ? $"{totalBytesToReceive / 1024.0:0.00} KB" 
                    : $"{totalBytesToReceive} Bytes";
            
            var downloadProgressText = $"{downloadProgressPercent:0.00}%";
            DownloadStatus = $"Downloaded {downloadProgressReceived} of {downloadProgressTotal} - {downloadProgressText} - {downloadSpeedText} - ETA: {FormatTimeSpan(eta)}";
            OnPropertyChanged(nameof(DownloadStatus));
        }
        
        public void UpdateDownloadStatus(bool status, bool isPaused, bool isFinished, bool isError, string errorMessage, ServerCommunication.GameStats game)
        {
            IsDownloading = status;
            OnPropertyChanged(nameof(IsDownloading));
            if (isPaused)
            {
                PlayButtonText = "Paused\nResume";
                OnPropertyChanged(nameof(PlayButtonText));
                game.Status = "Paused";
                GameTag = game;
                OnPropertyChanged(nameof(GameTag));
                return;
            }
            if (isFinished)
            {
                PlayButtonText = "Play";
                OnPropertyChanged(nameof(PlayButtonText));
                game.Status = "Play";
                GameTag = game;
                OnPropertyChanged(nameof(GameTag));
                return;
            }
            if (isError)
            {
                PlayButtonText = "Error\nRetry Download\n" + errorMessage;
                OnPropertyChanged(nameof(PlayButtonText));
                game.Status = "Download";
                GameTag = game;
                OnPropertyChanged(nameof(GameTag));
                return;
            }
            PlayButtonText = "Downloading...\nPause";
            game.Status = "Downloading";
            GameTag = game;
            OnPropertyChanged(nameof(PlayButtonText));
        }
        
        public void UpdateServerStatus(bool isServerError)
        {
            IsServerError = isServerError;
            OnPropertyChanged(nameof(IsServerError));
        }
        
        public void DisplayError(string errorMessage)
        {
            ServerErrorText = errorMessage;
            IsServerError = true;
            OnPropertyChanged(nameof(ServerErrorText));
            OnPropertyChanged(nameof(IsServerError));
        }
        
        public void EnableServerConnection()
        {
            IsServerConnected = true;
            Console.WriteLine("Server connected");
            OnPropertyChanged(nameof(IsServerConnected));
            UpdateUserStatus(IsLoggedIn);
        }
        
        public void DisableServerConnection()
        {
            IsServerConnected = false;
            Console.WriteLine("Server disconnected");
            OnPropertyChanged(nameof(IsServerConnected));
            UpdateUserStatus(IsLoggedIn);
        }

        public void ToggleFriendMenu(bool isFriendMenuVisible)
        {
            IsFriendMenuVisible = isFriendMenuVisible;
            OnPropertyChanged(nameof(IsFriendMenuVisible));
        }

        private void UpdateUserStatus(bool isLoggedIn)
        {
            if (IsServerConnected)
            {
                IsServerConnectedAndUserLoggedIn = isLoggedIn;
                IsServerConnectedAndUserNotLoggedIn = !isLoggedIn;
            } else
            {
                IsServerConnectedAndUserLoggedIn = false;
                IsServerConnectedAndUserNotLoggedIn = false;
            }

            OnPropertyChanged(nameof(IsServerConnectedAndUserLoggedIn));
            OnPropertyChanged(nameof(IsServerConnectedAndUserNotLoggedIn));
        }
        
        public void SetLoggedIn(bool isLoggedIn)
        {
            IsLoggedIn = isLoggedIn;
            UpdateUserStatus(isLoggedIn);
            OnPropertyChanged(nameof(IsLoggedIn));
        }

        public void ToggleFriendAddMenu(bool openStatus)
        {
            IsAddingFriend = openStatus;
            OnPropertyChanged(nameof(IsAddingFriend));
        }
        
        public string EnteredFriendUsername { get; set; } = string.Empty;
        public string OwnMessageColor { get; set; } = "#FF0000";
        public string FriendMessageColor { get; set; } = "#0000FF";


        [RelayCommand]
        private async Task AcceptFriendRequest(string userId)
        {
            Console.WriteLine("Acception friend request");
            try
            {
                var viewModel = this;
                await FriendUtils.AcceptFriendRequest(userId, viewModel);
                //updateUI();
            } 
            catch (Exception ex)
            {
                ToastNotification.Show("Failed to accept friend request: " + ex.Message);
                Console.WriteLine("Failed to accept friend request: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task CancelFriendRequest(string userId)
        {
            Console.WriteLine("Canceling friend request");
            try
            {
                var viewModel = this;
                Console.WriteLine(viewModel);
                await FriendUtils.CancelFriendRequest(userId, viewModel);
                //updateUI();
            } 
            catch (Exception ex)
            {
                ToastNotification.Show("Failed to cancel friend request: " + ex.Message);
                Console.WriteLine("Failed to cancel friend request: " + ex.Message);
            }
        }
        
        
        [RelayCommand]
        private async Task AddFriend()
        {
            Console.WriteLine("Adding friend.");
            var enteredUsername = EnteredFriendUsername;
            Console.WriteLine(enteredUsername);
            if (enteredUsername == "") return;
            try
            {
                var viewModel = this;
                Console.WriteLine(viewModel);
                await FriendUtils.SentFriendRequestToServer(enteredUsername, viewModel);
            } 
            catch (Exception ex)
            {
                Console.WriteLine("Failed to add friend: " + ex.Message);
            }
            EnteredFriendUsername = string.Empty;
            OnPropertyChanged(nameof(EnteredFriendUsername));
        }

        [RelayCommand]
        private async Task DeclineFriendRequest(string userId)
        {
            Console.WriteLine("Declining friend request");
            try
            {
                var viewModel = this;
                await FriendUtils.DeclineFriendRequest(userId, viewModel);
                //updateUI();
            } 
            catch (Exception ex)
            {
                ToastNotification.Show("Failed to decline friend request: " + ex.Message);
                Console.WriteLine("Failed to decline friend request: " + ex.Message);
            }
        }
        
        [RelayCommand]
        private async Task BlockUser(string userId)
        {
            Console.WriteLine("Blocking user");
            try
            {
                var viewModel = this;
                await FriendUtils.BlockUser(userId, viewModel);
            } 
            catch (Exception ex)
            {
                ToastNotification.Show("Failed to block user: " + ex.Message);
                Console.WriteLine("Failed to block user: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task UnblockUser(string userId)
        {
            Console.WriteLine("Unblocking user");
            try
            {
                var viewModel = this;
                await FriendUtils.UnblockUser(userId, viewModel);
            } 
            catch (Exception ex)
            {
                ToastNotification.Show("Failed to unblock user: " + ex.Message);
                Console.WriteLine("Failed to unblock user: " + ex.Message);
            }
        }
        

        [RelayCommand]
        private async Task RemoveFriend(string userId)
        {
            Console.WriteLine("Removing friend");
            try
            {
                var viewModel = this;
                await FriendUtils.RemoveFriend(userId, viewModel);
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Failed to remove friend: " + ex.Message);
                Console.WriteLine("Failed to remove friend: " + ex.Message);
            }
        }
        
        public async Task SendChatMessage(ChatUtils.ChatMessage message)
        {
            Console.WriteLine("Sending chat message");
            try
            {
                var viewModel = this;
                await ChatUtils.SendChatMessage(message, viewModel);
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Failed to send chat message: " + ex.Message);
                Console.WriteLine("Failed to send chat message: " + ex.Message);
            }
        }

        public void UpdateFriendRequestText()
        {
            var friendRequestAmount = ReceivedFriendRequests.Count;
            FriendRequestsAmountWithText = $"Friend Requests ({friendRequestAmount})";
            OnPropertyChanged(nameof(FriendRequestsAmountWithText));
        }

        public void ChangeFriendStatus(FriendUtils.Friend friend)
        {
            var friendIndex = FriendList.IndexOf(friend);
            if (friendIndex == -1) return;
            FriendList[friendIndex] = friend;
            OnPropertyChanged(nameof(FriendList));
        }

        public void UpdateFriendViews()
        {
            OnPropertyChanged(nameof(FriendList));
            OnPropertyChanged(nameof(SentFriendRequests));
            OnPropertyChanged(nameof(ReceivedFriendRequests));
            OnPropertyChanged(nameof(BlockedUsers));
            UpdateFriendRequestText(); 
        }

        public void UpdateSelectedChat(FriendUtils.Friend friend)
        {
            OnPropertyChanged(nameof(FriendList));
            Console.WriteLine($"Before Clear: {SelectedChatFriend.Count}");
            SelectedChatFriend.Clear();
            Console.WriteLine($"After Clear: {SelectedChatFriend.Count}");
            SelectedChatFriend.Add(friend);
            Console.WriteLine($"After ADD: {SelectedChatFriend.Count}");
            Console.WriteLine("Selected chat friend: " + friend.DisplayName);
            NoChatSelected = false;
            OnPropertyChanged(nameof(NoChatSelected));
            OnPropertyChanged(nameof(SelectedChatFriend));
        }

        public async Task LoadChatMessages(FriendUtils.Friend friend, int offset)
        {
            Console.WriteLine("Loading chat messages");
            try
            {
                IsLoadingMessages = true;
                var viewModel = this;
                await ChatUtils.LoadChatMessages(friend, offset, viewModel);
                IsLoadingMessages = false;
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Failed to load chat messages: " + ex.Message);
                Console.WriteLine("Failed to load chat messages: " + ex.Message);
                IsLoadingMessages = false;
            }
        }
    }
}
