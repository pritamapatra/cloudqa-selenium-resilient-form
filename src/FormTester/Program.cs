using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

var c = Config.Resolve(args);
using var d = new ChromeDriver();
var failed = 0;

void Log(string field, bool ok, string msg)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {field} | {msg}");
    if (!ok) failed++;
}

try
{
    d.Navigate().GoToUrl(c.Url);
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

    var g = RadioFinder.Find(d, "Gender", c.Gender);
    if (g.Element == null) Log("Gender", false, g.Error!);
    else
    {
        try { g.Element.Click(); }
        catch (WebDriverException) { ((IJavaScriptExecutor)d).ExecuteScript("arguments[0].click()", g.Element); }
        Log("Gender", g.Element.Selected, $"strategy={g.Strategy} confidence={g.Score} selected={g.Element.Selected}");
    }

    var sd = DropdownFinder.Find(d, "State");
    if (sd.Element == null) Log("State", false, sd.Error!);
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
            var actual = (string)((IJavaScriptExecutor)d).ExecuteScript(
                "return arguments[0].options[arguments[0].selectedIndex].text", sd.Element);
            Log("State", Matcher.Normalize(actual) == want, $"strategy={sd.Strategy} confidence={sd.Score} selected='{actual.Trim()}'");
        }
    }
}
finally
{
    d.Quit();
}
return failed == 0 ? 0 : 1;
