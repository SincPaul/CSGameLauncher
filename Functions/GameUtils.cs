using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GameLauncher.ViewModels;

namespace GameLauncher.Functions
{
    public class GameUtils
    { public static string GetLocalGameVersion(int gameId)
        {
            var gamePath = Path.Combine(Program.BasePath, "Games", gameId.ToString());
            var metadataPath = Path.Combine(gamePath, "metadata.json");
            Console.WriteLine("Game Path: " + gamePath);
            if (!File.Exists(metadataPath))
            {
                return string.Empty; 
            }

            try
            {
                var metadataJson = File.ReadAllText(metadataPath);
                var metadata = JsonSerializer.Deserialize<ServerCommunication.GameStats>(metadataJson);
                return metadata?.Version ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading game version for {gameId}: {ex.Message}");
                return string.Empty;
            }
        }

        public static string GetLocalGameHash(int gameId)
        {
            var gamePath = Path.Combine(Program.BasePath, "Games", gameId.ToString());
            var metadataPath = Path.Combine(gamePath, "metadata.json");

            if (!File.Exists(metadataPath))
            {
                return string.Empty;
            }

            try
            {
                var metadataJson = File.ReadAllText(metadataPath);
                var metadata = JsonSerializer.Deserialize<ServerCommunication.GameStats>(metadataJson);
                return metadata?.Hash ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading game hash for {gameId}: {ex.Message}");
                return string.Empty;
            }
        }
        
        public static void CreateGameMetadata(string filePath, ServerCommunication.GameStats game, string executable)
        {
            var metadataFile = Path.Combine(filePath, "metadata.json");
            var metadataJson = JsonSerializer.Serialize(game);
            File.WriteAllText(metadataFile, metadataJson);
            
            var gamePath = Path.Combine(Program.BasePath, "Games");
            if (!Path.Exists(gamePath))
            {
                Directory.CreateDirectory(gamePath);
            }
            var localGamesFile = Path.Combine(gamePath, "localgames.json");
            if (!File.Exists(localGamesFile))
            {
                File.WriteAllText(localGamesFile, "[]");
            }
            
            var localGamesJson = File.ReadAllText(localGamesFile);
            var localGames = JsonSerializer.Deserialize<List<GameId>>(localGamesJson) ?? [];
            if (localGames == null) throw new ArgumentNullException(nameof(localGames));
            var gameId = new GameId {Id = game.Id};
            localGames.Add(gameId);
            var newLocalGamesJson = JsonSerializer.Serialize(localGames);
            File.WriteAllText(localGamesFile, newLocalGamesJson);
        }

        private static string GetExecutable(int gameId)
        {
            var gamePath = Path.Combine(Program.BasePath, "Games", gameId.ToString());
            var metadataPath = Path.Combine(gamePath, "metadata.json");
            Console.WriteLine(gamePath);
            var metadataJson = File.ReadAllText(metadataPath);
            var metadata = JsonSerializer.Deserialize<ServerCommunication.GameStats>(metadataJson);
            Console.WriteLine(metadata);
            var executable = metadata?.Executable;
            if (string.IsNullOrEmpty(executable))
            {
                Console.WriteLine("Executable not found.");
                return string.Empty;
            }
            var executablePath = Path.Combine(gamePath, executable);
            Console.WriteLine(executablePath);
            if (File.Exists(executablePath)) return executablePath;
            
            Console.WriteLine("Executable not found.");
            return string.Empty;
        }
        
        public static void LaunchGame(ServerCommunication.GameStats game, MainViewModel viewModel)
        {
            var gamePath = Path.Combine(Program.BasePath, "Games", game.Id.ToString());
            var gameExecutable = GetExecutable(game.Id);
            if (!File.Exists(gameExecutable))
            {
                Console.WriteLine("Game executable not found.");
                return;
            }

            Console.WriteLine("Launching game: " + game.Name);
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = gameExecutable,
                        WorkingDirectory = gamePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    }
                };
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                viewModel.DisplayError("Game requires admin privileges to run.");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error launching game: {ex.Message}");
                viewModel.DisplayError("Error launching game: " + ex.Message);
            }
        }

        public async static Task<List<GameId>> GetLocalGames()
        {
            var games = new List<GameId>();
            var gamesPath = Path.Combine(Program.BasePath, "Games");
            if (!Directory.Exists(gamesPath))
            {
                return games;
            }
            var localGames = Path.Combine(gamesPath, "localgames.json");
            if (!File.Exists(localGames))
            {
                return games;
            }
            
            var localGamesJson = File.ReadAllText(localGames);
            var localGamesList = JsonSerializer.Deserialize<List<GameId>>(localGamesJson);

            return localGamesList;
        }

        public static ServerCommunication.GameStats GetLocalGameStats(int gameId)
        {
            var gamePath = Path.Combine(Program.BasePath, "Games", gameId.ToString());
            var metadataPath = Path.Combine(gamePath, "metadata.json");
            if (!File.Exists(metadataPath))
            {
                return null;
            }

            try
            {
                var metadataJson = File.ReadAllText(metadataPath);
                var metadata = JsonSerializer.Deserialize<ServerCommunication.GameStats>(metadataJson);
                return metadata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading game metadata for {gameId}: {ex.Message}");
                return null;
            }
            
        }
    }
    
    

    public class GameMetadata
    {
        public required string Version { get; set; }
        public required string GameHash { get; set; }
        public required bool ForceNewVersion { get; set; }
        public required string Executable { get; set; }
    }
    
    public class GameId
    {
        public required int Id { get; set; }
    }
}
