using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GameLauncher.ViewModels;

namespace GameLauncher.Functions
{
    public static class ServerCommunication
    {
        private const string ServerAddress = "http://10.31.1.41:80";
        private const string WebsocketAddress = "ws://10.31.1.41:80";

        private static readonly Dictionary<string, Func<MainViewModel, string, Task>> WebsocketHandlers = new()
        {
            { $"{WebsocketAddress}/Server/OnlineStatus", async (viewModel, address) => await CheckServerOnlineStatus(viewModel, address) },
        };

        private static async Task CheckServerOnlineStatus(MainViewModel viewModel, string address)
        {
            var websocketAddress = new Uri(address);

            try
            {
                using var client = new ClientWebSocket();
                await client.ConnectAsync(websocketAddress, CancellationToken.None);
                viewModel.EnableServerConnection();
                Console.WriteLine("Connected to WebSocket server.");
            
                var buffer = new byte[1024];
                var segment = new ArraySegment<byte>(buffer);
            
                while (client.State == WebSocketState.Open)
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    try
                    {
                        var result = await client.ReceiveAsync(segment, cts.Token);
            
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine("WebSocket closed by server.");
                            viewModel.DisableServerConnection();
                            break;
                        }
            
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine("Received: " + message);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Receive operation timed out.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection error: " + e.Message);
                viewModel.DisableServerConnection();
            }

            // Schedule reconnection after 60 seconds
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                await CheckServerOnlineStatus(viewModel, address);
            });
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
                if (gameStats == null) throw new ArgumentNullException(nameof(gameStats));

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
}

    internal struct User
    {
        public string UUID { get; set; }
        public string DisplayName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
