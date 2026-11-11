using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

var c = Config.Resolve(args);
using var d = new ChromeDriver();
try
{
    d.Navigate().GoToUrl(c.Url);
    var r = TextFieldFinder.Find(d, "First Name");
    if (r.Element == null)
    {
        Console.WriteLine($"FAIL First Name: {r.Error}");
        return 1;
    }
    r.Element.Clear();
    r.Element.SendKeys(c.FirstName);
    var actual = r.Element.GetAttribute("value");
    var ok = actual == c.FirstName;
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} First Name | strategy={r.Strategy} confidence={r.Score} | value='{actual}'");
    return ok ? 0 : 1;
}
finally
{
    d.Quit();
}
