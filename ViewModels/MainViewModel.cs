using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameLauncher.Functions;

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

        public string PlayButtonText { get; set; } = "Loading Games";

        public bool VisibilityStatus => _isloading;

        public int GameId { get; set; } = -1;
        
        public ServerCommunication.GameStats? GameTag { get; set; } 

        public ObservableCollection<ServerCommunication.GameStats> GameList { get; } = [];
        public ObservableCollection<ServerCommunication.GameStats> OfflineGameList { get; } = [];
        public ObservableCollection<ServerCommunication.GameStats> OnlineGameList { get; } = [];
        
        public List<FriendUtils.Friend> FriendList { get; } = FriendUtils.GetFriends();
        
        public string LastPatch { get; set; } = string.Empty;

        public string SelectedGame { get; set; } = string.Empty;
        
        public string DownloadStatus { get; set; } = string.Empty;
        
        public bool IsDownloading { get; set; } = false;
        
        public bool IsServerError { get; set; } = false;
        
        public string ServerErrorText { get; set; } = "Couldnt connect to server";
        
        public bool IsServerConnected { get; set; } = false;
        public bool IsServerConnectedAndUserLoggedIn { get; set; } = false;
        public bool IsServerConnectedAndUserNotLoggedIn { get; set; } = false;
        
        public bool ButtonEnabled { get; set; } = false;
        public bool IsFriendMenuVisible { get; set; } = false;
        public bool IsLoggedIn { get; set; } = false;
        
        [RelayCommand]
        private void SwapLoadingStatus()
        {
            _isloading = !_isloading;
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
            GameId = game.Id;
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
        
        public void UpdateDownloadProgress(long LastSecondBytesReceived, long TotalBytesReceived, long TotalBytesToReceive)
        {
            long downloadSpeed = 0;
            var eta = TimeSpan.Zero;
            if (LastSecondBytesReceived != 0)
            {
                downloadSpeed = LastSecondBytesReceived / 1024;
                eta = TimeSpan.FromSeconds((TotalBytesToReceive - TotalBytesReceived) / LastSecondBytesReceived);
            }
            var downloadSpeedText = downloadSpeed > 1024 ? $"{downloadSpeed / 1024} MB/s" : $"{downloadSpeed} KB/s";
            var downloadProgressPercent = (double)TotalBytesReceived / TotalBytesToReceive * 100;
            var downloadProgressReceived = TotalBytesReceived >= 1024 * 1024 * 1024 ? $"{TotalBytesReceived / (1024 * 1024 * 1024.0):0.00} GB"
                    : TotalBytesReceived >= 1024 * 1024 ? $"{TotalBytesReceived / (1024 * 1024.0):0.00} MB" 
                    : TotalBytesReceived >= 1024 ? $"{TotalBytesReceived / 1024.0:0.00} KB" : $"{TotalBytesReceived} Bytes";
                var downloadProgressTotal = TotalBytesToReceive >= 1024 * 1024 * 1024 ? $"{TotalBytesToReceive / (1024 * 1024 * 1024.0):0.00} GB" 
                    : TotalBytesToReceive >= 1024 * 1024 ? $"{TotalBytesToReceive / (1024 * 1024.0):0.00} MB" 
                    : TotalBytesToReceive >= 1024 ? $"{TotalBytesToReceive / 1024.0:0.00} KB" 
                    : $"{TotalBytesToReceive} Bytes";
            
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
    }
}
