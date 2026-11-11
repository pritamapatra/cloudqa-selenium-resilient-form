using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

ChromeDriver? driver = null;
var failed = 0;

void Log(string field, bool ok, string msg)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {field} | {msg}");
    if (!ok) failed++;
}

try
{
    var c = Config.Resolve(args);
    var d = new ChromeDriver();
    driver = d;
    d.Navigate().GoToUrl(c.Url);
    var saveIdx = Array.IndexOf(args, "--save-html");
    if (saveIdx >= 0 && saveIdx + 1 < args.Length) File.WriteAllText(args[saveIdx + 1], d.PageSource);
    if (args.Contains("--diag")) Console.WriteLine(DropdownFinder.Dump(d, "State"));

    var t = TextFieldFinder.Find(d, "First Name");
    if (t.Element == null) Log("First Name", false, t.Error!);
    else
    {
        t.Element.Clear();
        t.Element.SendKeys(c.FirstName);
        var actual = t.Element.GetAttribute("value");
        Log("First Name", actual == c.FirstName, $"strategy={t.Strategy} confidence={t.Score} value='{actual}'");
    }

    var formScope = t.Element == null ? null :
        ((IJavaScriptExecutor)d).ExecuteScript("return arguments[0].form;", t.Element) as IWebElement;
    var g = RadioFinder.Find(d, "Gender", c.Gender, formScope);
    if (args.Contains("--diag") && g.Element != null)
        Console.WriteLine("RADIO DIAG | " +
            ((IJavaScriptExecutor)d).ExecuteScript(
                "var e=arguments[0]; return JSON.stringify({id:e.id,name:e.name,formId:e.form?e.form.id:null});",
                g.Element));
    if (g.Element == null) Log("Gender", false, g.Error!);
    else
    {
        try { g.Element.Click(); }
        catch (WebDriverException) { ((IJavaScriptExecutor)d).ExecuteScript("arguments[0].click()", g.Element); }
        Log("Gender", g.Element.Selected, $"strategy={g.Strategy} confidence={g.Score} selected={g.Element.Selected}");
    }

    var sd = DropdownFinder.Find(d, "State");
    if (sd.Element == null) Log("State", false, sd.Error!);
    else if (!sd.Element.TagName.Equals("select", StringComparison.OrdinalIgnoreCase))
    {
        var combo = sd.Element;
        var controls = combo.GetAttribute("aria-controls");
        if (string.IsNullOrWhiteSpace(controls))
            Log("State", false, "ARIA combobox has no aria-controls; unsupported popup association");
        else
        {
            combo.Click();
            var deadline = DateTime.UtcNow.AddSeconds(5);
            var matches = new List<IWebElement>();
            var available = new List<string>();
            do
            {
                matches.Clear();
                available.Clear();
                foreach (var popupId in controls.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var popup = ((IJavaScriptExecutor)d).ExecuteScript(
                        "return document.getElementById(arguments[0]);", popupId) as IWebElement;
                    if (popup == null || !popup.Displayed) continue;
                    foreach (var option in popup.FindElements(By.CssSelector("[role='option']")))
                    {
                        if (!option.Displayed || !option.Enabled ||
                            option.GetAttribute("aria-disabled") == "true") continue;
                        available.Add(option.Text.Trim());
                        if (Matcher.Normalize(option.Text) == Matcher.Normalize(c.State) &&
                            !matches.Contains(option))
                            matches.Add(option);
                    }
                }
                if (matches.Count > 0) break;
                Thread.Sleep(100);
            } while (DateTime.UtcNow < deadline);

            if (matches.Count == 0)
                Log("State", false,
                    $"ARIA option '{c.State}' not found (available: {string.Join(", ", available.Take(8))})");
            else if (matches.Count > 1)
                Log("State", false, $"ambiguous ARIA option '{c.State}': {matches.Count} matches");
            else
            {
                matches[0].Click();
                var actual = "";
                var verified = false;
                deadline = DateTime.UtcNow.AddSeconds(5);
                do
                {
                    actual = combo.TagName.Equals("input", StringComparison.OrdinalIgnoreCase)
                        ? combo.GetAttribute("value") ?? ""
                        : combo.Text.Trim();
                    verified = Matcher.Normalize(actual) == Matcher.Normalize(c.State);
                    if (verified) break;
                    Thread.Sleep(100);
                } while (DateTime.UtcNow < deadline);
                Log("State", verified,
                    $"strategy={sd.Strategy}+aria-combobox confidence={sd.Score} selected='{actual}'");
            }
        }
    }
    else
    {
        var options = sd.Element.FindElements(By.TagName("option"));
        var want = Matcher.Normalize(c.State);
        var match = options.FirstOrDefault(o => Matcher.Normalize(o.Text) == want);
        if (match == null)
        {
            var names = string.Join(", ", options.Take(8).Select(o => o.Text.Trim()));
            Log("State", false, $"option '{c.State}' not in list of {options.Count} options (first: {names})");
        }
        else
        {
            match.Click();
            var actual = ((IJavaScriptExecutor)d).ExecuteScript(
                "return arguments[0].options[arguments[0].selectedIndex].text", sd.Element)?.ToString()
                ?? throw new InvalidOperationException("Selected option text could not be read.");
            Log("State", Matcher.Normalize(actual) == want, $"strategy={sd.Strategy} confidence={sd.Score} selected='{actual.Trim()}'");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FAIL Runner | {ex.GetType().Name}: {ex.Message}");
    failed++;
}
finally
{
    if (driver != null)
    {
        try { driver.Dispose(); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL Cleanup | {ex.GetType().Name}: {ex.Message}");
            failed++;
        }
    }
}
return failed == 0 ? 0 : 1;
