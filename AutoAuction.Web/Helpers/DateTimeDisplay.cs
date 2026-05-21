namespace AutoAuction.Web.Helpers;

public static class DateTimeDisplay
{
    public static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static DateTime ToLocal(DateTime value)
    {
        return AsUtc(value).ToLocalTime();
    }

    public static string ToUtcIsoString(DateTime value)
    {
        return AsUtc(value).ToString("O");
    }
}
