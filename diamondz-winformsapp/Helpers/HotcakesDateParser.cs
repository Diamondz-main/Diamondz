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
            return null;

        if (!long.TryParse(match.Groups[1].Value, out long milliseconds))
            return null;

        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime;
    }
}
