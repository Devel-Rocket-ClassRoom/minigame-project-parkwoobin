using System;

public static class TimeUtil
{
    public static long NowUnixMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public static DateTime FromUnixMillis(long millis)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(millis).LocalDateTime;
    }

    public static string ToDateString(long millis)
    {
        return FromUnixMillis(millis).ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>초를 현재 언어에 맞는 시간 형식으로 변환. 해당 단위가 0이면 생략.</summary>
    public static string FormatClearTime(float seconds)
    {
        int h = (int)(seconds / 3600);
        int m = (int)(seconds % 3600 / 60);
        int s = (int)(seconds % 60);

        bool isKorean = LanguageManager.CurrentLanguage == LanguageManager.Language.Korean;
        if (isKorean)
        {
            if (h > 0) return $"{h}시간 {m}분 {s:D2}초";
            if (m > 0) return $"{m}분 {s:D2}초";
            return $"{s}초";
        }
        else
        {
            if (h > 0) return $"{h}h {m}m {s:D2}s";
            if (m > 0) return $"{m}m {s:D2}s";
            return $"{s}s";
        }
    }
}
