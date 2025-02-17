using System.Collections.Generic;

namespace GameLauncher.Functions;

public class FriendUtils
{
    public class Friend
    {
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
    }
    
    public static List<Friend> GetFriends()
    {
        var friends = new List<Friend>();
        friends.Add(new Friend { UserName = "TestUser", DisplayName = "Test User", UserId = "1", Status = "Online" });
        friends.Add(new Friend { UserName = "TestUser2", DisplayName = "Test User 2", UserId = "2", Status = "Offline" });
        friends.Add(new Friend { UserName = "TestUser3", DisplayName = "Test User 3", UserId = "3", Status = "Online" });
        friends.Add(new Friend { UserName = "TestUser4", DisplayName = "Test User 4", UserId = "4", Status = "Offline" });
        return friends;
    }
}