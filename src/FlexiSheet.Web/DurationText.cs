using System.Globalization;
using FlexiSheet.Domain;

namespace FlexiSheet.Web;

public static class DurationText
{
    public static string Format(TimeSpan value) => FlexiSheet.Application.DurationFormatter.Format(value);

    public static TimeSpan Parse(string text, bool allowNegative = false)
    {
        var value = text.Trim();
        var negative = value.StartsWith('-');
        if (negative)
            value = value[1..];
        var parts = value.Split(':');
        var maxMinutes = TimeSpan.MaxValue.Ticks / TimeSpan.TicksPerMinute;
        if ((negative && !allowNegative) || parts.Length != 2 || parts[0].Length == 0 ||
            !parts[0].All(char.IsAsciiDigit) || parts[1].Length != 2 ||
            !parts[1].All(char.IsAsciiDigit) ||
            !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours) ||
            !int.TryParse(parts[1], out var minutes) || minutes > 59 ||
            hours > (maxMinutes - minutes) / 60)
            throw new RuleViolationException(allowNegative
                ? "Enter carry-over as hours:minutes, for example 7:30 or -2:15."
                : "Enter expected time as hours:minutes, for example 7:30 or 0:00.");

        var ticks = (hours * 60 + minutes) * TimeSpan.TicksPerMinute;
        return new TimeSpan(negative ? -ticks : ticks);
    }

    public static TimeSpan ParseTime(string text, bool allowEndOfDay = false)
    {
        var value = Parse(text);
        if (value > TimeSpan.FromDays(1) || (!allowEndOfDay && value == TimeSpan.FromDays(1)))
            throw new RuleViolationException(allowEndOfDay
                ? "Enter an end time from 0:00 to 24:00."
                : "Enter a start time from 0:00 to 23:59.");
        return value;
    }
}
