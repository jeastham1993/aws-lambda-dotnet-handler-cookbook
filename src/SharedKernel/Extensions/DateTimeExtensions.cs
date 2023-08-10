namespace SharedKernel.Extensions;

public static class DateTimeExtensions
{
    public static int ToEpochTime(this DateTime dateTime)
    {
        TimeSpan t = dateTime - new DateTime(1970, 1, 1);
        return (int)t.TotalSeconds;
    }
}