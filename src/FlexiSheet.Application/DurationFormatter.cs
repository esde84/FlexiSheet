namespace FlexiSheet.Application;

public static class DurationFormatter
{
    public static string Format(TimeSpan value)
    {
        var minutes = value.Ticks / TimeSpan.TicksPerMinute;
        return FormattableString.Invariant(
            $"{(minutes < 0 ? "-" : "")}{Math.Abs(minutes) / 60}:{Math.Abs(minutes) % 60:00}");
    }
}
