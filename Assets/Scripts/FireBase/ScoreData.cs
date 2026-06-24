using System;

[Serializable]
public class ScoreData
{
    public float clearTime;
    public long timestamp;

    public ScoreData() { }

    public ScoreData(float clearTime, long timestamp)
    {
        this.clearTime = clearTime;
        this.timestamp = timestamp;
    }

    public DateTime GetDateTime()
    {
        return TimeUtil.FromUnixMillis(timestamp);
    }

    public string GetDateString()
    {
        return TimeUtil.ToDateString(timestamp);
    }
}
