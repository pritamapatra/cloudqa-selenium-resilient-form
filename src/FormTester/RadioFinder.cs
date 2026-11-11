using OpenQA.Selenium;

public static class RadioFinder
{
    const string Js = @"
var e = arguments[0];
function t(n) { return n ? (n.innerText || n.textContent || '').trim() : ''; }
function vis(n) { var p = n.parentElement; return p && p.tagName !== 'SCRIPT' && p.tagName !== 'STYLE' && p.offsetParent !== null; }
function prevText(el) {
  var w = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  var last = '';
  while (w.nextNode()) {
    var n = w.currentNode;
    if (el.compareDocumentPosition(n) & Node.DOCUMENT_POSITION_FOLLOWING) break;
    var s = n.textContent.trim();
    if (s && vis(n)) last = s;
  }
  return last;
}
function nextText(el) {
  var w = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  while (w.nextNode()) {
    var n = w.currentNode;
    if (!(el.compareDocumentPosition(n) & Node.DOCUMENT_POSITION_FOLLOWING) || el.contains(n)) continue;
    var s = n.textContent.trim();
    if (s && vis(n)) return s;
  }
  return '';
}
var r = {};
r.forLabel = e.labels ? Array.from(e.labels).map(t).join(' ') : '';
r.wrap = t(e.closest('label'));
r.aria = e.getAttribute('aria-label') || '';
r.value = e.value || '';
r.id = e.id || '';
r.next = nextText(e);
var g = Array.from(document.querySelectorAll('input[type=radio]')).filter(function (x) { return e.name && x.name === e.name && x.form === e.form; });
var first = g.length ? g[0] : e;
r.gPrev = prevText(first);
var fs = e.closest('fieldset');
r.legend = fs ? t(fs.querySelector('legend')) : '';
var rg = e.closest('[role=radiogroup]');
r.gAria = rg ? (rg.getAttribute('aria-label') || '') : '';
r.gName = e.name || '';
return r;";

    static readonly (double Base, string Key)[] OptionSources =
    {
        (100, "forLabel"), (90, "wrap"), (90, "aria"), (70, "value"), (70, "id"), (50, "next")
    };

    static readonly (double Base, string Key)[] GroupSources =
    {
        (100, "legend"), (90, "gAria"), (70, "gName"), (50, "gPrev")
    };

    public static FindResult Find(IWebDriver d, string groupLabel, string option, IWebElement? scope = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        FindResult result;
        do
        {
            result = Attempt(d, groupLabel, option, scope);
            if (result.Element != null) return result;
            Thread.Sleep(500);
        } while (DateTime.UtcNow < deadline);
        return result;
    }

    static FindResult Attempt(IWebDriver d, string groupLabel, string option, IWebElement? scope)
    {
        var js = (IJavaScriptExecutor)d;
        var scored = new List<(IWebElement El, double Opt, double Grp, string Strategy)>();

        foreach (var el in ((ISearchContext?)scope ?? d).FindElements(By.CssSelector("input[type='radio']")))
        {
            if (!el.Displayed || !el.Enabled) continue;
            var r = (IDictionary<string, object>?)js.ExecuteScript(Js, el) ?? new Dictionary<string, object>();
            string S(string k) => r.TryGetValue(k, out var v) ? v?.ToString() ?? "" : "";
            var opt = OptionSources.Select(s => (Score: Matcher.Score(s.Base, S(s.Key), option), s.Key))
                                   .OrderByDescending(x => x.Score).First();
            var grp = GroupSources.Select(s => Matcher.Score(s.Base, S(s.Key), groupLabel)).Max();
            if (opt.Score > 0) scored.Add((el, opt.Score, grp, opt.Key));
        }

        scored = scored.OrderByDescending(x => x.Opt).ThenByDescending(x => x.Grp).ToList();
        var top = string.Join(", ", scored.Take(3).Select(s => $"{s.Strategy}:{s.Opt}/group:{s.Grp}"));

        if (scored.Count == 0 || scored[0].Opt < Matcher.Threshold)
            return new(null, "", scored.Count == 0 ? 0 : scored[0].Opt,
                $"no confident radio '{option}' in group '{groupLabel}' (best: {(top == "" ? "none" : top)})");
        if (scored.Count > 1 && scored[1].Opt == scored[0].Opt && scored[1].Grp == scored[0].Grp)
            return new(null, "", scored[0].Opt, $"ambiguous radio '{option}' in group '{groupLabel}' ({top}) {Describe(scored[0].El)} / {Describe(scored[1].El)}");

        return new(scored[0].El, $"{scored[0].Strategy}+group:{scored[0].Grp}", scored[0].Opt, null);
    }

    static string Describe(IWebElement el) =>
        $"[id={el.GetAttribute("id")} name={el.GetAttribute("name")} value={el.GetAttribute("value")} at={el.Location.X},{el.Location.Y}]";
}
