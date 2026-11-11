using System.Text.Json;

public record Config(string Url, string FirstName, string Gender, string State)
{
    public const string DefaultUrl = "https://app.cloudqa.io/home/AutomationPracticeForm";

    static string? Arg(string[] a, string name)
    {
        var i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
    }

    static string Pick(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";

    public static Config Resolve(string[] args)
    {
        var path = Arg(args, "--data") ?? "testdata.json";
        Dictionary<string, string> file = new(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(path))
            file = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                   ?? file;
        string F(string k) => file.TryGetValue(k, out var v) ? v : "";

        return new Config(
            Pick(Arg(args, "--url"), Environment.GetEnvironmentVariable("TARGET_URL"), DefaultUrl),
            Pick(Arg(args, "--first-name"), F("firstName"), "Pritam"),
            Pick(Arg(args, "--gender"), F("gender"), "Male"),
            Pick(Arg(args, "--state"), F("state"), "Karnataka"));
    }
}
