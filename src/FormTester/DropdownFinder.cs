using OpenQA.Selenium;

public static class DropdownFinder
{
    const string Js = @"
var e = arguments[0];
function t(n) { return n ? (n.innerText || n.textContent || '').trim() : ''; }
var r = {};
r.forLabel = e.labels ? Array.from(e.labels).filter(function (l) { return l.htmlFor; }).map(t).join(' ') : '';
r.wrap = t(e.closest('label'));
r.aria = e.getAttribute('aria-label') || '';
var lb = e.getAttribute('aria-labelledby');
r.labelledby = lb ? lb.split(/\s+/).map(function (i) { return t(document.getElementById(i)); }).join(' ') : '';
r.title = e.getAttribute('title') || '';
r.name = e.getAttribute('name') || '';
r.id = e.id || '';
var w = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
var last = '';
while (w.nextNode()) {
  var n = w.currentNode;
  if (e.compareDocumentPosition(n) & Node.DOCUMENT_POSITION_FOLLOWING) break;
  var s = n.textContent.trim();
  var p = n.parentElement;
  if (s && p && p.tagName !== 'SCRIPT' && p.tagName !== 'STYLE' && p.offsetParent !== null) last = s;
}
r.prev = last;
return r;";

    static readonly (double Base, string Key)[] Sources =
    {
        (100, "forLabel"), (90, "wrap"), (90, "aria"), (90, "labelledby"),
        (70, "title"), (70, "name"), (70, "id"), (50, "prev")
    };

    public static FindResult Find(IWebDriver d, string label)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        FindResult result;
        do
        {
            result = Attempt(d, label);
            if (result.Element != null) return result;
            Thread.Sleep(500);
        } while (DateTime.UtcNow < deadline);
        return result;
    }

    static FindResult Attempt(IWebDriver d, string label)
    {
        var js = (IJavaScriptExecutor)d;
        var scored = new List<(IWebElement El, double Score, string Strategy)>();

        foreach (var el in d.FindElements(By.CssSelector("select")))
        {
            if (!el.Displayed || !el.Enabled) continue;
            var r = (IDictionary<string, object>?)js.ExecuteScript(Js, el) ?? new Dictionary<string, object>();
            string S(string k) => r.TryGetValue(k, out var v) ? v?.ToString() ?? "" : "";
            var best = Sources.Select(s => (Score: Matcher.Score(s.Base, S(s.Key), label), s.Key))
                              .OrderByDescending(x => x.Score).First();
            if (best.Score > 0) scored.Add((el, best.Score, best.Key));
        }

        scored = scored.OrderByDescending(x => x.Score).ToList();
        var top = string.Join(", ", scored.Take(3).Select(s => $"{s.Strategy}:{s.Score}"));

        if (scored.Count == 0 || scored[0].Score < Matcher.Threshold)
            return new(null, "", scored.Count == 0 ? 0 : scored[0].Score,
                $"no confident dropdown for '{label}' (best: {(top == "" ? "none" : top)})");
        if (scored.Count > 1 && scored[1].Score == scored[0].Score)
            return new(null, "", scored[0].Score,
                $"ambiguous dropdown for '{label}' ({top}) {Key(scored[0].El)} / {Key(scored[1].El)}");

        return new(scored[0].El, scored[0].Strategy, scored[0].Score, null);
    }

    static string Key(IWebElement el) => $"[id={el.GetAttribute("id")} name={el.GetAttribute("name")}]";

    public static string Dump(IWebDriver d, string label)
    {
        var js = (IJavaScriptExecutor)d;
        var lines = new List<string>();
        foreach (var el in d.FindElements(By.CssSelector("select")))
        {
            var r = (IDictionary<string, object>?)js.ExecuteScript(Js, el) ?? new Dictionary<string, object>();
            string S(string k) => r.TryGetValue(k, out var v) ? v?.ToString() ?? "" : "";
            var best = Sources.Select(s => (Score: Matcher.Score(s.Base, S(s.Key), label), s.Key))
                              .OrderByDescending(x => x.Score).First();
            var opts = el.FindElements(By.TagName("option"));
            lines.Add($"shown={el.Displayed} id={S("id")} name={S("name")} n={opts.Count} first='{opts.FirstOrDefault()?.Text.Trim()}' " +
                      $"forLabel='{S("forLabel")}' prev='{S("prev")}' score={best.Score} via={best.Key} at={el.Location.Y}");
        }
        return string.Join("\n", lines);
    }
}
