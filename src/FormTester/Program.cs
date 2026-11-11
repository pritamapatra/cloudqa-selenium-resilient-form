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
}
finally
{
    d.Quit();
}
return failed == 0 ? 0 : 1;
