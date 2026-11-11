using System.Text.Json;

public record Config(string Url, string FirstName, string Gender, string State)
{
    public const string DefaultUrl = "https://app.cloudqa.io/home/AutomationPracticeForm";

    static string Pick(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";

    public static Config Resolve(string[] args)
    {
        var valueFlags = new HashSet<string>(StringComparer.Ordinal)
        {
            "--url", "--data", "--first-name", "--gender", "--state", "--save-html"
        };
        var parsed = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var i = 0; i < args.Length; i++)
        {
            var flag = args[i];
            if (flag != "--diag" && !valueFlags.Contains(flag))
                throw new ArgumentException($"Unknown argument: {flag}");
            if (parsed.ContainsKey(flag))
                throw new ArgumentException($"Repeated argument: {flag}");

            if (flag == "--diag")
            {
                parsed.Add(flag, "");
                continue;
            }

            if (i + 1 >= args.Length || args[i + 1].StartsWith("--") ||
                string.IsNullOrWhiteSpace(args[i + 1]))
                throw new ArgumentException($"Missing or empty value for {flag}");

            parsed.Add(flag, args[++i]);
        }

        string? Arg(string flag) =>
            parsed.TryGetValue(flag, out var value) ? value : null;

        var path = Arg("--data") ?? "testdata.json";
        Dictionary<string, string> file = new(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(path) && parsed.ContainsKey("--data"))
            throw new FileNotFoundException($"Test data file not found: {path}");

        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(path));
            if (loaded == null)
                throw new ArgumentException("Test data must be a JSON object, not null.");
            file = new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
        }

        string? F(string key) => file.TryGetValue(key, out var value) ? value : null;

        var url = Pick(Arg("--url"), Environment.GetEnvironmentVariable("TARGET_URL"), DefaultUrl);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https" && uri.Scheme != "file"))
            throw new ArgumentException("URL must be an absolute http, https, or file URL.");

        return new Config(
            url,
            Pick(Arg("--first-name"), F("firstName"), "Pritam"),
            Pick(Arg("--gender"), F("gender"), "Male"),
            Pick(Arg("--state"), F("state"), "Canada"));
    }
}
