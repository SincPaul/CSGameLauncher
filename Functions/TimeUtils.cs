using System;

namespace GameLauncher.Functions;

public class TimeUtils
{
    public static string RelativeTimeAgoConverter(int timestamp)
    {
        var time = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        var timeSpan = DateTimeOffset.Now - time;
        switch (timeSpan.TotalDays)
        {
            case > 365:
                return $"{(int)(timeSpan.TotalDays / 365)} years ago";
            case > 30:
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";
            case > 7:
                return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
            case > 1:
                return $"{(int)timeSpan.TotalDays} days ago";
        }

        if (timeSpan.TotalHours > 1) return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalMinutes > 1) return $"{(int)timeSpan.TotalMinutes} minutes ago";
        return timeSpan.TotalSeconds > 1 ? $"{(int)timeSpan.TotalSeconds} seconds ago" : "Just now";
    }
}