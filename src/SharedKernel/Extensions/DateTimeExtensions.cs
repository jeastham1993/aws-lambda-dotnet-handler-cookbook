namespace SharedKernel.Extensions;

public static class DateTimeExtensions
{
    public static int ToEpochTime(this DateTime dateTime)
    {
        TimeSpan t = dateTime - DateTime.UnixEpoch;
        return (int)t.TotalSeconds;
    }
}