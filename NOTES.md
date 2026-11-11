# Approach and Limitations

## Approach

The application discovers visible, enabled candidates by control type rather
than using fixed target-field IDs, classes, or positional XPath expressions.

Each finder collects semantic signals and ranks candidates:
- Explicit associated label: 100.
- Wrapping label or supported ARIA naming signal: 90.
- Supported attribute signals such as name, ID, title, placeholder, or
  radio value: 70.
- Nearby text fallback: 50.

Radio group signals use legend (100), radiogroup aria-label (90),
name (70), and preceding text (50). Radio options are ranked by option
score first, then group score.

Matching normalizes case, punctuation, whitespace, and camel-case boundaries.
Exact matches receive the source weight; whole-phrase substring matches
receive 60% of that weight. The acceptance threshold is 50.

Equal top-ranked candidates are rejected, including candidates with identical
IDs and names. Scores are heuristic weights, not probabilities.

## Field operations

- First Name: clear, type, and compare the resulting value.
- Gender: click the selected candidate and verify Selenium's Selected state.
  A JavaScript click is attempted if the normal click throws WebDriverException.
- Native State dropdown: choose an exact normalized option-text match and
  read back selected option text.
- ARIA State combobox: click, inspect visible enabled options within the
  aria-controls targets, reject duplicate option matches, select, and
  verify displayed text or input value.

The runner reports field results and returns 0 only when no failure was recorded.
Configuration, browser, and cleanup exceptions produce a clear runner/cleanup
error and exit 1.

## Duplicate controls and form scoping

The practice page contains repeated IDs across its sections.

Initial renamed-attribute testing exposed a false positive: Gender selected
an unchanged radio from shadowdomautomationtestform instead of the main form.
The diagnostic showed the incorrect owning form.

Radio discovery now uses the matched First Name control's owning form when
available. The renamed variant then selected the renamed main-form radio.
No specific form ID is hard-coded.

This is a contextual assumption: the intended Gender control is expected to
belong to the same form as First Name. When no owning form is available,
radio discovery falls back to document-wide candidates.

Text and dropdown discovery remain document-wide. Form scoping is not a
universal guarantee that every selected field belongs to the same form.

## Verification performed

- Original live page and saved baseline passed.
- Five mutation fixtures passed: reordered fields, renamed attributes,
  added wrappers, look-alike text fields, and a second dropdown.
- All five were rerun after strict ambiguity rejection was introduced.
- Duplicate-State negative fixture returned an ambiguity failure and exit 1.
- ARIA fixture selected Canada and verified read-back.
- Missing ARIA option returned a clear failure and exit 1.
- Native baseline passed after the ARIA implementation.
- Renamed fixture passed after runner/configuration hardening.
- Invalid CLI cases returned clear failures and exit 1.

After the ARIA addition, all eight saved fixtures were rerun:
the baseline, five mutation variants, and ARIA fixture exited 0;
the ambiguous-State fixture exited 1. Expected exit codes matched 8/8.

These checks were executed through a sequential subprocess loop that compared
exit codes. Per-field console output was also reviewed; no automated browser-state
assertion suite has been added.

## Limitations

- No iframe traversal or shadow-root traversal.
- English-oriented field targets; normalization preserves only ASCII letters
  and digits, so non-English labels are not reliably supported.
- Heavy redesign, nonstandard widgets, or weak semantic naming can fail.
- Nearby-text fallbacks use DOM order, not measured visual distance.
- Radio group score is a ranking tiebreaker, not an independently enforced
  minimum; an option match can succeed without strong group evidence.
- Radio wrapping labels may receive the explicit-label weight because the
  radio finder collects all element.labels under its forLabel source.
- Radio discovery does not currently resolve aria-labelledby.
- The JavaScript radio-click fallback can bypass normal interactability
  checks; Selected verifies state, not realistic user accessibility.
- ARIA support requires aria-controls and visible role=option descendants.
  Virtualized, type-to-search, keyboard-only, dynamically associated, and
  other combobox patterns are not comprehensively supported.
- ARIA read-back checks displayed text/input value, not application persistence.
- ARIA option collection stops when a match is available; later asynchronous
  duplicates are not guaranteed to be detected.
- Native duplicate option text is not explicitly rejected.
- A field-operation exception ends the run; remaining fields may not be reported.
- No form submission, backend persistence check, or screenshot evidence.
- Saved HTML retains third-party scripts/styles, duplicate IDs, and baseline
  hidden verification tokens. It is not a sanitized, self-contained offline app.
- Finder retry windows do not impose a global wall-clock timeout on navigation
  or browser startup.
- A previously measured full run took approximately 58 seconds; the cause was
  not conclusively established. Architecture detection alone was not proven
  to explain the delay.

## Implementation versus original plan

The implementation targets net10.0 and was built with SDK 10.0.404,
rather than the originally planned .NET 8.

The code uses static finders and a top-level runner rather than the complete
interface-based component structure proposed in the architecture document.
Native and ARIA interaction logic currently lives in Program.cs.
