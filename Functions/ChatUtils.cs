using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GameLauncher.ViewModels;
using GameLauncher.Windows;

namespace GameLauncher.Functions;

public class ChatUtils
{
    public class ChatMessage
    {
        public required string Message { get; set; }
        public required string SenderId { get; set; }
        public required string ReceiverId { get; set; }
        public required DateTime TimeSentTimestamp { get; set; }
        public int MessageId { get; set; }
        public string Color { get; set; }
    }
    
    public static async Task SendChatMessage(ChatUtils.ChatMessage chatMessage, MainViewModel viewModel)
    {
        Console.WriteLine("Sending Chat Message: " + chatMessage.Message);
        try
        {
            var wsmessage = new
            {
                wstype = "chat",
                type = "chat_message", 
                
                message = chatMessage
            };
            
            await viewModel.chatClient.SendAsync(JsonSerializer.Serialize(wsmessage));
        }
        catch (Exception ex)
        {
            ToastNotification.Show("Failed to send chat message: " + ex.Message);
            Console.WriteLine("Failed to send chat message: " + ex.Message);
        }
    }

    public static async Task LoadChatMessages(FriendUtils.Friend friend, int offset, MainViewModel viewModel)
    {
        Console.WriteLine("Loading chat messages for friend: " + friend.DisplayName);
        try
        {
            var wsMessage = new
            {
                wstype = "chat",
                type = "load_chat_messages_with_user",
                offset = offset,
                friendId = friend.UserId,
                userId = viewModel.MainUser.uuid
            };

            var wsMessageJson = JsonSerializer.Serialize(wsMessage);
            await viewModel.chatClient.SendAsync(wsMessageJson);

        }
        catch (Exception ex)
        {
            ToastNotification.Show("Failed to load chat messages: " + ex.Message);
            Console.WriteLine("Failed to load chat messages: " + ex.Message);
        }

    }

    public static void HandleChatWsMessage(string message, MainViewModel viewModel)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message) || message[0] != '{')
            {
                Console.WriteLine("Ignoring non-JSON message: " + message);
                return;
            }

            var jsonDocument = JsonDocument.Parse(message);
            var type = jsonDocument.RootElement.GetProperty("type").GetString();
            Console.WriteLine("Handling chat message: " + type);

            var handlers = new Dictionary<string, Action<JsonDocument, MainViewModel>>
            {
                { "loaded_chat_messages", HandleLoadedChatMessages },
                { "chat_message", HandleChatMessage }
            };
            
            if (handlers.TryGetValue(type, out var handler))
            {
                handler(jsonDocument, viewModel);
            }
            else
            {
                Console.WriteLine("Unknown chat message type: " + type);
            }
        }
        catch (Exception ex)
        {
            ToastNotification.Show("Failed to handle chat message: " + ex.Message);
            Console.WriteLine("Failed to handle chat message: " + ex.Message);
        }
    }
    
    private static void HandleLoadedChatMessages(JsonDocument jsonDocument, MainViewModel viewModel)
    {
        Console.WriteLine("Handling loaded chat messages");
        try
        {
            Console.WriteLine(1);
            var messages = jsonDocument.RootElement.GetProperty("messages").EnumerateArray();
            Console.WriteLine(1);
            var friendId = jsonDocument.RootElement.GetProperty("friendId").GetString();
            Console.WriteLine(1);
            var offset = jsonDocument.RootElement.GetProperty("offset").GetInt32();
            Console.WriteLine(1);
            var amount = jsonDocument.RootElement.GetProperty("amount").GetInt32();

            Console.WriteLine(1);
            var friend = viewModel.FriendList.FirstOrDefault(f => f.UserId == friendId);
            if (friend == null)
            {
                Console.WriteLine("Friend not found: " + friendId);
                ToastNotification.Show("Failed to load chat messages: Friend not found.");
                return;
            }

            var friendMessages = friend.ChatMessages;
            Console.WriteLine(1);

            foreach (var message in messages)
            {
                if (!message.TryGetProperty("Message", out var messageProp) ||
                    !message.TryGetProperty("SenderId", out var senderIdProp) ||
                    !message.TryGetProperty("ReceiverId", out var receiverIdProp) ||
                    !message.TryGetProperty("TimeSentTimestamp", out var timeSentProp) ||
                    !message.TryGetProperty("MessageId", out var messageIdProp))
                {
                    Console.WriteLine("Skipping message due to missing properties.");
                    continue;
                }
                
                var chatMessage = new ChatMessage
                 {
                     Message = message.GetProperty("Message").GetString() ?? "",
                     SenderId = message.GetProperty("SenderId").GetString() ?? "",
                     ReceiverId = message.GetProperty("ReceiverId").GetString() ?? "",
                     TimeSentTimestamp = DateTime.Parse(message.GetProperty("TimeSentTimestamp").GetString() ?? ""),
                     MessageId = message.GetProperty("MessageId").GetInt32()
                 };
                
                AddToMessages(friendMessages, chatMessage, viewModel);
                
            }

            viewModel.UpdateSelectedChat(friend);
        }
        catch (Exception ex)
        {
            ToastNotification.Show("Failed to handle loaded chat messages: " + ex.Message);
            Console.WriteLine("Failed to handle loaded chat messages: " + ex.Message);
            return;
        }
    }

    private static void HandleChatMessage(JsonDocument jsonDocument, MainViewModel viewModel)
    {
        Console.WriteLine("Received/Sent Message");
        try
        {
            var message = jsonDocument.RootElement.GetProperty("message");
            if (!message.TryGetProperty("Message", out var messageProp) ||
                !message.TryGetProperty("SenderId", out var senderIdProp) ||
                !message.TryGetProperty("ReceiverId", out var receiverIdProp) ||
                !message.TryGetProperty("TimeSentTimestamp", out var timeSentProp) ||
                !message.TryGetProperty("MessageId", out var messageIdProp))
            {
                Console.WriteLine("Skipping message due to missing properties.");
                return;
            }
            
            var chatMessage = new ChatMessage
            {
                Message = message.GetProperty("Message").GetString() ?? "",
                SenderId = message.GetProperty("SenderId").GetString() ?? "",
                ReceiverId = message.GetProperty("ReceiverId").GetString() ?? "",
                TimeSentTimestamp = DateTime.Parse(message.GetProperty("TimeSentTimestamp").GetString() ?? ""),
                MessageId = int.Parse(message.GetProperty("MessageId").GetString() ?? "0"),
            };
            
            var friend = viewModel.FriendList.FirstOrDefault(f => f.UserId == chatMessage.SenderId || f.UserId == chatMessage.ReceiverId);
            if (friend == null)
            {
                Console.WriteLine("Friend not found: " + chatMessage.SenderId + " " + chatMessage.ReceiverId);
                ToastNotification.Show("Failed to load chat messages: Friend not found.");
                return;
            }
            
            var friendMessages = friend.ChatMessages;
            
            AddToMessages(friendMessages, chatMessage, viewModel);
            
            viewModel.UpdateSelectedChat(friend);
        }
        catch (Exception ex)
        {
            ToastNotification.Show("Failed to handle chat message: " + ex.Message);
            Console.WriteLine("Failed to handle chat message: " + ex.Message);
        }
    }

    private static void AddToMessages(ObservableCollection<ChatMessage> friendMessages, ChatMessage chatMessage, MainViewModel viewModel)
    {
        if (chatMessage.SenderId == viewModel.MainUser.uuid)
        {
            chatMessage.Color = viewModel.OwnMessageColor;
        } 
        else
        {
            chatMessage.Color = viewModel.FriendMessageColor;
        }
        
        friendMessages.Add(chatMessage);
    }
}