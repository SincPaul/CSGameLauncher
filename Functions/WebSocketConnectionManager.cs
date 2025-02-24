using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameLauncher.ViewModels;

namespace GameLauncher.Functions;

public class WebSocketConnectionManager
{
    private ClientWebSocket _webSocket;
    private bool _isConnecting;
    private bool _receivedPong;
    
    private readonly Func<MainViewModel, Task> _onEnable;
    private readonly Action<MainViewModel> _onDisable;

    public WebSocketConnectionManager(
        Func<MainViewModel, Task> onEnable, 
        Action<MainViewModel> onDisable)
    {
        _onEnable = onEnable;
        _onDisable = onDisable;
    }
    
    public async Task ConnectAsync(MainViewModel viewModel, string address)
    {
        if (_isConnecting) return;

        try
        {
            _isConnecting = true;
            _webSocket = new ClientWebSocket();
            if (UserUtils.UserCookie != string.Empty)
            {
                _webSocket.Options.SetRequestHeader("Cookie", UserUtils.UserCookie);
            }

            var webSocketUri = new Uri(address);
            await _webSocket.ConnectAsync(webSocketUri, CancellationToken.None);

            await _onEnable(viewModel);
            Console.WriteLine($"Connected to websocket {address}");

            _receivedPong = true;
            _ = ListenAsync(viewModel);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to connect to websocket: " + ex.Message);
            _onDisable(viewModel);
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private async Task ListenAsync(MainViewModel viewModel)
    {
        var buffer = new byte[1024];
        try
        {
            var ReceiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var message = Encoding.UTF8.GetString(buffer, 0, ReceiveResult.Count);
            Console.WriteLine("Received message: " + message);

            if (ReceiveResult.MessageType == WebSocketMessageType.Close)
            {
                _onDisable(viewModel);
                return;
            }

            if (message == "pong")
            {
                _receivedPong = true;
                return;
            }

            Console.WriteLine("Received unexpected message: " + message);

        }
        catch(Exception ex)
        {
            Console.WriteLine("Failed to receive message: " + ex.Message);
            _onDisable(viewModel);
        }
    }
}