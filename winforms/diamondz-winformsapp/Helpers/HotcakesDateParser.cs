using System.Globalization;
using System.Text.RegularExpressions;

namespace DiamondzWinForms.Helpers;

public static class HotcakesDateParser
{
    private static readonly Regex DateRegex = new(@"/Date\((\d+)\)/", RegexOptions.Compiled);

    public static DateTime? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var match = DateRegex.Match(raw);
        if (!match.Success)
            return TryParseTextDate(raw);

        if (!long.TryParse(match.Groups[1].Value, out long milliseconds))
            return TryParseTextDate(raw);

        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime;
    }

    private static DateTime? TryParseTextDate(string raw)
    {
        foreach (var culture in new[] { CultureInfo.InvariantCulture, new CultureInfo("en-US"), new CultureInfo("hu-HU") })
        {
            if (DateTime.TryParse(raw, culture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var parsed))
                return parsed;
        }

        return null;
    }
}
