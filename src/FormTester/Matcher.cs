using System.Text.RegularExpressions;

public static class Matcher
{
    public const double Threshold = 50;

    public static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = Regex.Replace(s, "([a-z0-9])([A-Z])", "$1 $2");
        s = Regex.Replace(s.ToLowerInvariant(), "[^a-z0-9]+", " ");
        return s.Trim();
    }

    public static double Score(double baseScore, string? text, string target)
    {
        var t = Normalize(text);
        var g = Normalize(target);
        if (t == "" || g == "") return 0;
        if (t == g) return baseScore;
        if ((" " + t + " ").Contains(" " + g + " ")) return baseScore * 0.6;
        return 0;
    }
}
