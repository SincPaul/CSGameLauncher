using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GameLauncher.Functions;
using GameLauncher.ViewModels;

namespace GameLauncher
{
    public partial class MainView : Window
    {
        private static ObservableCollection<ServerCommunication.GameStats> Games { get; } = [];

        public bool IsLoading { get; set; } = true;

        public MainView()
        {
            InitializeComponent();
            Console.WriteLine("Hello, World!");
            Loaded += LoadedMainView;
        }

        public void LoadedMainView(object? sender, RoutedEventArgs e)
        {
            InitializeData();
        }
        
        private async void InitializeData()
        {
            try
            {
                // await Task.Delay(10000);
                var viewModel = DataContext as MainViewModel;
                await ServerCommunication.HandleWebsockets(viewModel);
                Console.WriteLine(viewModel);
                await LoadGamesAsync(viewModel);
                SwapLoading(null, null);
                Console.WriteLine("Finished loading Games");

                (DataContext as MainViewModel)?.ShowGames(Games);

                if (Games.Count > 0)
                {
                    (DataContext as MainViewModel)?.SwapMainGameCommand?.Execute(Games[0]);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to load games: " + e.Message);
            }
        }
        
        private async Task LoadGamesAsync(MainViewModel viewModel)
        {
            if (viewModel == null) {
                Console.WriteLine("AHHHHHHHHHHHHHHHHHHHHHHHH");
                return;
            }
            Console.WriteLine(viewModel);
            var gamesFromServer = await ServerCommunication.CheckServerGames(viewModel);
            var localGames = await GameUtils.GetLocalGames();
            Games.Clear();
            foreach (var localGame in localGames)
            {
                var gameStats = GameUtils.GetLocalGameStats(localGame.Id);
                Games.Add(gameStats);
                Console.WriteLine("Local game added: " + gameStats.Name);
            }
            foreach (var game in gamesFromServer)
            {
                var isLocalGame = false;
                foreach (var localGame in localGames)
                {
                    if (game.Id != localGame.Id) continue;
                    isLocalGame = true;
                    break;
                }

                if (isLocalGame) continue;
                Games.Add(game);
                Console.WriteLine(game);
                Console.WriteLine("Game added: " + game.Name);
            }
            Console.WriteLine(Games);
            IsLoading = false;
        }

        private void SwapLoading(object? sender, RoutedEventArgs? e)
        {
            Console.WriteLine("Swapping loading status.");
            
            (DataContext as MainViewModel)?.SwapLoadingStatusCommand?.Execute(null);
        }

        private void ChooseGame(object? sender, RoutedEventArgs e)
        {
            var game = (sender as Button)?.Tag as ServerCommunication.GameStats;
            Console.WriteLine(DataContext as MainViewModel);
            (DataContext as MainViewModel)?.SwapMainGameCommand?.Execute(game);
        }

        private async void LauncherGameCommand(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine(sender); 
            Console.WriteLine($"Button Content: {(sender as Button)?.Content}");
            Console.WriteLine($"Button Name: {(sender as Button)?.Name}");
            Console.WriteLine($"Button Tag: {(sender as Button)?.Tag}");
            Console.WriteLine($"Button IsEnabled: {(sender as Button)?.IsEnabled}");
            Console.WriteLine($"Button IsVisible: {(sender as Button)?.IsVisible}");
            var game = (sender as Button)?.Tag as ServerCommunication.GameStats;
            
            Console.WriteLine("Launching game: " + game?.Name);
            Console.WriteLine(game);
            
            Console.WriteLine("Status of game: " + game?.Status);
            if (game == null) return;
            Console.WriteLine("Game status: " + game.Status);
            var viewModel = DataContext as MainViewModel;
            Console.WriteLine(viewModel);
            switch (game.Status)
            {
                case "Play":
                    Console.WriteLine("Launching game: " + game.Name);
                    if (viewModel == null) return;
                    GameUtils.LaunchGame(game, viewModel);
                    break;
                case "Update":
                    Console.WriteLine("Updating game: " + game.Name);
                    break;
                case "Download":
                    Console.WriteLine("Downloading game: " + game.Name);
                    if (viewModel == null) return;
                    await ServerCommunication.DownloadGame(game, viewModel);
                    break;
                case "Error":
                    Console.WriteLine("Error launching game: " + game.Name);
                    break;
                case "Downloading":
                    Console.WriteLine("Pausing download for game: " + game.Name);
                    break;
                case "Paused":
                    Console.WriteLine("Resuming download for game: " + game.Name);
                    break;
                default:
                    Console.WriteLine("Unknown game status: " + game.Status);
                    break;
            }
            
        }



        

    }
}
