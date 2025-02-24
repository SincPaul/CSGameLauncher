using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GameLauncher.Functions;
using GameLauncher.ViewModels;
using GameLauncher.Windows;

namespace GameLauncher.Views
{
    public partial class MainView : Window
    {
        

        public MainView()
        {
            InitializeComponent();
            Console.WriteLine("Hello, World!");
            Loaded += LoadedMainView;
        }

        private void LoadedMainView(object? sender, RoutedEventArgs e)
        {
            InitializeData();
        }
        
        private async void InitializeData()
        {
            try
            {
                // await Task.Delay(10000);
                if (DataContext is not MainViewModel viewModel) return;
                _ = Task.Run(() => ServerCommunication.HandleWebsockets(viewModel));
                Console.WriteLine(viewModel);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to initialize data: " + e.Message);
            }
        }
        
        

        private void ChooseGame(object? sender, RoutedEventArgs e)
        {
            var game = (sender as Button)?.Tag as ServerCommunication.GameStats;
            Console.WriteLine(DataContext as MainViewModel);
            (DataContext as MainViewModel)?.SwapMainGameCommand.Execute(game);
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
