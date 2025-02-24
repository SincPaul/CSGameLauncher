using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using GameLauncher.ViewModels;

namespace GameLauncher.Functions
{
    public static class ServerCommunication
    {
        private static ObservableCollection<GameStats> Games { get; } = [];
        public const string ServerAddress = "http://10.31.1.41:80";
        public const string WebsocketAddress = "ws://10.31.1.41:80";
        private static ClientWebSocket? _serverStatusWebSocket;
        private static bool _isConnecting = false;
        private static bool _receivedPong = true;
        private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

        private static readonly Dictionary<string, Func<MainViewModel, string, Task>> WebsocketHandlers = new()
        {
            { $"{WebsocketAddress}/Server/OnlineStatus", async (viewModel, address) => await MonitorServerStatus(viewModel, address) }
            //{ $"{WebsocketAddress}/friends", async (viewModel, address) => await MonitorFriends(viewModel, address) }
        };
        
        

        private static async Task MonitorServerStatus(MainViewModel viewModel, string address)
        {
            var serverClient = new WebSocketClient();
            serverClient.OnMessage += message =>
            {
                if (message == "pong") return;
                Console.WriteLine("Received Server message: " + message);
            };
            serverClient.OnConnect += async () =>
            {
                Console.WriteLine("Connected to server.");
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    viewModel.EnableServerConnection();
                    await UserUtils.CreateUser(viewModel);
                    await GameUtils.LoadGamesAsync(viewModel, Games);
                    viewModel.ShowGames(Games);
                    viewModel.ChangeLoadingStatus(false);
                    Console.WriteLine("Finished loading Games");
                
                    if (Games.Count > 0)
                    {
                        viewModel.SwapMainGameCommand.Execute(Games[0]);
                    }
                });
            };
            var address1 = address;
            serverClient.OnDisconnect += async () =>
            {
                Console.WriteLine("Disconnected from server.");
                viewModel.DisableServerConnection();
                UserUtils.RemoveUser(viewModel);
                await Task.Delay(ReconnectDelay);
                await serverClient.ReconnectAsync(address1);
            };
            await serverClient.ConnectAsync(address1);
            
            address = $"{WebsocketAddress}/friends";

            var friendClient = new WebSocketClient();
            friendClient.Option.SetRequestHeader("Cookie", UserUtils.UserCookie);
            friendClient.OnMessage += message =>
            {
                if (message == "pong") return;
                Console.WriteLine("Received Friend message: " + message);
                FriendUtils.HandleFriendWsMessage(message, viewModel);
            };
            friendClient.OnConnect += async () =>
            {
                Console.WriteLine("Connected to friends.");
                await FriendUtils.LoadFriendStuff(viewModel);

            };
            friendClient.OnDisconnect += async () =>
            {
                Console.WriteLine("Disconnected from friends.");
                await Task.Delay(ReconnectDelay);
                await friendClient.ReconnectAsync(address);
            };
            await friendClient.ConnectAsync(address);
            

            //if (!viewModel.IsServerConnected)
            //{
            //    await TryConnect(viewModel, address);
            //}
            //else
            //{
            //    await SendPingAndCheckPong(viewModel);
            //}
        }

        private static async Task TryConnect(MainViewModel viewModel, string address)
        {
            if (_isConnecting) return;

            try
            {
                _isConnecting = true;
                _serverStatusWebSocket?.Dispose();
                _serverStatusWebSocket = new ClientWebSocket();

                var websocketAddress = new Uri(address);
                await _serverStatusWebSocket.ConnectAsync(websocketAddress, CancellationToken.None);
                viewModel.EnableServerConnection();
                Console.WriteLine("Connected to WebSocket server.");

                _receivedPong = true; 
                _ = ListenForMessages(viewModel); 
                
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await UserUtils.CreateUser(viewModel);
                    await GameUtils.LoadGamesAsync(viewModel, Games);
                    viewModel.ShowGames(Games);
                    viewModel.ChangeLoadingStatus(false);
                    Console.WriteLine("Finished loading Games");
                
                    if (Games.Count > 0)
                    {
                        viewModel.SwapMainGameCommand.Execute(Games[0]);
                    }
                });

            }
            catch (Exception e)
            {
                viewModel.DisableServerConnection();
                UserUtils.RemoveUser(viewModel);
            }
            finally
            {
                _isConnecting = false;
            }
        }
        
        //private static async Task MonitorFriends(MainViewModel viewModel, string address)
        //{
        //    if (UserUtils.UserCookie == string.Empty)
        //    {
        //        Console.WriteLine("No user cookie found.");
        //        viewModel.DisableFriendsConnection();
        //        return;
        //    }
        //    
        //    if (!viewModel.IsFriendsConnected)
        //    {
        //        await TryConnectFriends(viewModel, address);
        //    }
        //    else
        //    {
        //        await SendPingAndCheckPongFriends(viewModel);
        //    }
        //}
        

        private static async Task SendPingAndCheckPong(MainViewModel viewModel)
        {
            if (_serverStatusWebSocket is not { State: WebSocketState.Open })
            {
                viewModel.DisableServerConnection();
                return;
            }

            if (!_receivedPong)
            {
                Console.WriteLine("No Pong received, assuming disconnect.");
                viewModel.DisableServerConnection();
                return;
            }

            try
            {
                var buffer = Encoding.UTF8.GetBytes("ping");
                await _serverStatusWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                //Console.WriteLine("Sent ping to WebSocket server.");

                _receivedPong = false; 
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ping failed: " + ex.Message);
                viewModel.DisableServerConnection();
            }
        }

        private static async Task ListenForMessages(MainViewModel viewModel)
        {
            var buffer = new byte[1024];

            try
            {
                while (_serverStatusWebSocket is { State: WebSocketState.Open })
                {
                    var result = await _serverStatusWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if (message == "pong")
                    {
                        // Console.WriteLine("Received Pong from server.");
                        _receivedPong = true;
                    }
                    else
                    {
                        Console.WriteLine("Received unexpected message: " + message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error receiving message: " + ex.Message);
                viewModel.DisableServerConnection();
            }
        }
        
        


        public class GameList
        {
            public required int Id { get; set; }
            public required string Name { get; set; }
        }
        
        public class GameStats
        {
            public required string Name { get; set; }
            public required string PatchName { get; set; }
            public required string Version { get; set; }
            public required string Hash { get; set; }
            public required int Id { get; set; }
            public required int Size { get; set; }
            public required int UploadedAt { get; set; }
            public required bool ForceNewVersion { get; set; }
            public string Executable { get; set; } = "game.exe";
            public string Status { get; set; } = "Play";
            public bool IsValid { get; set; } = false;
        }

        private static async Task<List<GameList>> GetServerGames()
        {
            var gameFileAddress = new Uri($"{ServerAddress}/GameLauncher/GameList");
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(gameFileAddress);

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine(json);

            var gamesDictionary = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
            Console.WriteLine(gamesDictionary);
            
            var games = gamesDictionary?
                .Select(kvp => new GameList { Id = kvp.Key, Name = kvp.Value })
                .ToList();
            Console.WriteLine("Games found: " + games?.Count);
            return games ?? [];
        }


        public static async Task<List<GameStats>> CheckServerGames(MainViewModel viewModel)
        {
            
            var finalGamesList = new List<GameStats>();
            try
            {
                var gamesNoData = await GetServerGames();
                Console.WriteLine("Games found on server: " + string.Join(", ", gamesNoData));
                foreach (var game in gamesNoData)
                {
                    Console.WriteLine("ALL GUD");
                    var gameId = game.Id;
                    Console.WriteLine("TSET0: gameID: " + gameId);
                    var serverGameStats = await GetGameStats(gameId);
                    Console.WriteLine("TEST");
                    if (serverGameStats == null) continue;
                    var gameStats = GetIndividualGameStats(serverGameStats);
                    Console.WriteLine("TEST2");
                    if (gameStats.IsValid != true) continue;
                    Console.WriteLine("ALL GUD2");
                    finalGamesList.Add(gameStats);
                }

                return finalGamesList;
            }
            catch (HttpRequestException ex)
            {
                viewModel.UpdateServerStatus(true);
                Console.WriteLine($"1An error occurred while sending the request: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
            
            return [];
        }

        private static async Task<GameStats?> GetGameStats(int gameId)
        {
            var gameStatsAddress = new Uri($"{ServerAddress}/GameLauncher/GameStats/{gameId}");
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(gameStatsAddress);
            Console.WriteLine(response);
            if (!response.IsSuccessStatusCode)
                return null;
            
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine(json);
            var gameStats = JsonSerializer.Deserialize<GameStats>(json);
            return gameStats;
        }

        private static GameStats GetIndividualGameStats(GameStats serverGameStats)
        {
            var gameName = serverGameStats.Name;
            var patchName = serverGameStats.PatchName;
            var serverGameVersion = serverGameStats.Version;
            var serverGameHash = serverGameStats.Hash;
            var gameId = serverGameStats.Id;
            var gameSize = serverGameStats.Size;
            var gameUploadedAt = serverGameStats.UploadedAt;
            var forceNewVersion = serverGameStats.ForceNewVersion;
            var executable = serverGameStats.Executable;
            
            
            var localGameVersion = GameUtils.GetLocalGameVersion(gameId);
            var localGameHash = GameUtils.GetLocalGameHash(gameId);
            
            var needsDownload = false;
            var needsUpdate = false;
            var isValid = false;
            var hasError = false;
            
            if (string.IsNullOrEmpty(localGameVersion) || string.IsNullOrEmpty(localGameHash))
            {
                needsDownload = true;
                isValid = true;
            }
            else if (localGameVersion != serverGameVersion)
            {
                needsUpdate = true;
                isValid = true;
            }
            else if (localGameHash != serverGameHash)
            {
                hasError = true;
            }
            else
            {
                isValid = true;
            }
            
            var status = needsDownload ? "Download" : (needsUpdate ? "Update" : (hasError ? "Error" : "Play"));

            return new GameStats
            {
                Name = gameName,
                PatchName = patchName,
                Version = localGameVersion, 
                Hash = localGameHash,
                Id = gameId,
                Size = gameSize,
                UploadedAt = gameUploadedAt,
                ForceNewVersion = forceNewVersion,
                Status = status,
                IsValid = isValid,
                Executable = executable
            };
        }
        
        public static async Task DownloadGame(GameStats game, MainViewModel viewModel)
        {
            try
            {
                viewModel.UpdateDownloadStatus(true, false, false, false, "", game);
                var gameId = game.Id;
                Console.WriteLine("Starting Download");
                var gameDownloadAddress = new Uri($"{ServerAddress}/GameLauncher/Download/{gameId}");
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(gameDownloadAddress, HttpCompletionOption.ResponseHeadersRead);
        
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error downloading game.");
                    viewModel.UpdateDownloadStatus(false, false, false, true, "", game);
                    return;
                }

                var gamePath = Path.Combine(Program.BasePath, "Games", gameId.ToString());
                if (!Directory.Exists(gamePath))
                {
                    Directory.CreateDirectory(gamePath);
                }

                var gameStats = await GetGameStats(gameId);
                if (gameStats == null)
                {
                    //throw new ArgumentNullException(nameof(gameStats));
                    Console.WriteLine("Game stats are null.");
                    viewModel.UpdateDownloadStatus(false, false, false, true, "", game);
                }

                var contentDisposition = response.Content.Headers.ContentDisposition;
                var fileName = contentDisposition?.FileName?.Trim('"') ?? "game.exe";
                var gameFilePath = Path.Combine(gamePath, fileName);

                var fileSize = response.Content.Headers.ContentLength ?? 0;
                Console.WriteLine("File Size: " + fileSize);
                viewModel.UpdateDownloadProgress(0, 0, fileSize);

                await using var gameFileStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(gameFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        
                var buffer = new byte[8192];
                int bytesRead;
                long totalBytesRead = 0;
                var lastProgressUpdate = DateTime.UtcNow;
                long lastProgressBytes = 0;

                while ((bytesRead = await gameFileStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalBytesRead += bytesRead;

                    var timeDiff = DateTime.UtcNow - lastProgressUpdate;
                    if (!(timeDiff.TotalSeconds >= 1)) continue;
                    
                    var bytesPerSecond = totalBytesRead - lastProgressBytes;
                    lastProgressBytes = totalBytesRead;
                    Console.WriteLine("Bytes per second: " + bytesPerSecond);
                    viewModel.UpdateDownloadProgress(bytesPerSecond, totalBytesRead, fileSize);
                    lastProgressUpdate = DateTime.UtcNow;
                }

                viewModel.UpdateDownloadStatus(false, false, true, false, "", game);
                GameUtils.CreateGameMetadata(gamePath, gameStats, fileName);
                Console.WriteLine("Game downloaded.");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Access to the path is denied: " + ex.Message);
                var errorMsg = "Access to the path is denied: " + ex.Message;
                viewModel.UpdateDownloadStatus(false, false, false, true, errorMsg, game);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error downloading game: " + ex.Message);    
                var errorMsg = "Error downloading game: " + ex.Message;
                viewModel.UpdateDownloadStatus(false, false, false, true, errorMsg, game);
            }
        }
        
        
        public static async Task HandleWebsockets(MainViewModel viewModel)
        {

            foreach (var (address, handlerFunc) in WebsocketHandlers)
            {
                await handlerFunc(viewModel, address);
            }

            
        }
        
        
    }

    public class WebSocketClient
    {
        private ClientWebSocket _socket;
        public ClientWebSocketOptions Option => _socket.Options;
        private readonly TimeSpan _pingInterval = TimeSpan.FromSeconds(5);
        private Task _pingTask;
        public event Action<string> OnMessage;
        public event Action OnDisconnect;
        public event Action OnConnect;
        public event Action OnClose;

        public WebSocketClient()
        {
            try
            {
                _socket = new ClientWebSocket();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create websocket: " + ex.Message);
            }
        }

        public async Task ConnectAsync(string address)
        {
            try
            {
                var uri = new Uri(address);
                await _socket.ConnectAsync(uri, CancellationToken.None);
                _ = ListenAsync();
                _pingTask = StartPingLoopAsync();
                OnConnect?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to websocket: " + ex.Message);
                OnDisconnect?.Invoke();
            }
        }
        
        private async Task StartPingLoopAsync()
        {
            while (_socket.State == WebSocketState.Open)
            {
                await Task.Delay(_pingInterval);
                await SendAsync("ping");
            }
        }
        
        public async Task SendAsync(string message)
        {
            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending message: " + ex.Message);
            }
        }
        
        private async Task ListenAsync()
        {
            var buffer = new byte[1024];
            try
            {
                while (_socket.State == WebSocketState.Open)
                {
                    var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessage?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error receiving message: " + ex.Message);
                OnDisconnect?.Invoke();
                // reconnect
            }
        }

        public async Task DisconnectAsync()
        {
            try 
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                OnDisconnect?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error disconnecting from websocket: " + ex.Message);
            }
        }

        public async Task ReconnectAsync(string address)
        {
            try
            {
                if (_socket != null && _socket.State != WebSocketState.Closed)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None);
                    _socket.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error closing websocket: " + ex.Message);
            }

            _socket = new ClientWebSocket();
            _socket.Options.SetRequestHeader("Cookie", UserUtils.UserCookie);
    
            try
            {
                var uri = new Uri(address);
                await _socket.ConnectAsync(uri, CancellationToken.None);
                _ = ListenAsync();
                _pingTask = StartPingLoopAsync();
                OnConnect?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect: " + ex.Message);
                OnDisconnect?.Invoke();
            }
        }
    }
}

    internal struct User
    {
        public string UUID { get; set; }
        public string DisplayName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
