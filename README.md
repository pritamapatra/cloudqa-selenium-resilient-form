# CloudQA Resilient Form Tester

C# and Selenium console application that locates First Name, Gender, and State
using ranked semantic signals, fills configured values, and verifies read-back.

## Requirements

- .NET 10 SDK; project targets `net10.0`.
- Google Chrome.
- Network access for initial NuGet restore and browser-driver resolution.

Verified development environment: .NET SDK 10.0.404 and
Selenium.WebDriver 4.52.0. The original project plan proposed .NET 8;
the implemented project uses .NET 10.

## Build and run

From the repository root:

```bash
cd src/FormTester
dotnet restore
dotnet build -v q
dotnet bin/Debug/net10.0/FormTester.dll
echo "exit=$?"
```

The default target is:
https://app.cloudqa.io/home/AutomationPracticeForm

Selenium Manager handles ChromeDriver resolution. No driver path is hard-coded.

## Configuration

URL precedence: `--url` > `TARGET_URL` > default URL.

Field-value precedence: CLI override > test-data JSON > built-in default.

Built-in defaults:
- First Name: Pritam
- Gender: Male
- State: Canada

By default, the application reads `testdata.json` from the working directory.
An absent default file uses built-in defaults. An explicitly requested missing
file fails.

```bash
dotnet bin/Debug/net10.0/FormTester.dll \
  --url "[https://app.cloudqa.io/home/AutomationPracticeForm](https://app.cloudqa.io/home/AutomationPracticeForm)" \
  --data testdata.json \
  --first-name Pritam --gender Male --state Canada
```

JSON format:

```json
{"firstName":"Pritam","gender":"Male","state":"Canada"}
```

The practice page labels its dropdown State but lists countries.
Canada is a verified option; Karnataka is not available.

## Diagnostics

From `src/FormTester`:

```bash
dotnet bin/Debug/net10.0/FormTester.dll --diag
dotnet bin/Debug/net10.0/FormTester.dll --save-html ../../variants/captured.html
```

`--diag` prints dropdown candidates and the selected radio's identity.
`--save-html PATH` saves the DOM page source after navigation, before filling.
The destination directory must already exist.

Unknown or repeated arguments, missing/empty values, invalid URL schemes,
and explicitly missing data files produce errors.

## Local mutation tests

From `src/FormTester`:

```bash
for variant in original reordered renamed-attributes wrappers look-alikes second-dropdown aria-combobox; do
  printf '\n--- %s ---\n' "$variant"
  dotnet bin/Debug/net10.0/FormTester.dll \
    --url "file://$(cd ../../variants && pwd)/${variant}.html"
  echo "exit=$?"
done
```

Verified passing fixtures:
- `original.html`: saved baseline.
- `reordered.html`: reordered field containers.
- `renamed-attributes.html`: main-form IDs, names, and classes renamed;
  label references and radio grouping preserved.
- `wrappers.html`: additional control wrappers.
- `look-alikes.html`: Middle Name and another Last Name before First Name.
- `second-dropdown.html`: Shipping Country dropdown, also containing Canada.
- `aria-combobox.html`: custom State combobox with an associated listbox.

The five planned mutation variants passed after strict tie rejection was added.
The ARIA fixture additionally passed, followed by a native-baseline regression.
Saved fixtures retain external scripts/styles and are not fully offline-isolated.

## Negative checks

From `src/FormTester`:

```bash
dotnet bin/Debug/net10.0/FormTester.dll \
  --url "file://$(cd ../../variants && pwd)/ambiguous-state.html"
echo "exit=$?"

dotnet bin/Debug/net10.0/FormTester.dll \
  --url "file://$(cd ../../variants && pwd)/aria-combobox.html" --state Narnia
echo "exit=$?"
```

Both should exit `1`: ambiguous State candidates and an absent ARIA option.

Other verified failures: absent native options, missing fields on example.com,
unknown flags, missing/empty values, repeated flags, missing explicit data files,
and invalid URLs.

## Output and exit codes

- Per-field PASS/FAIL, with strategy and score on successful matches.
- Scores are ranking weights, not calibrated probabilities.
- `0`: all fields passed and cleanup succeeded.
- `1`: field failure or caught configuration/browser/cleanup error.

## Scope

Supports text inputs, radio buttons, native selects, and a limited ARIA
combobox pattern using `aria-controls` and visible `role="option"` descendants.
ARIA selection is verified using displayed text or an input value.

Radio candidates are scoped to the matched First Name control's owning form
when available. Equal top-ranked candidates fail rather than selecting by DOM order.

No iframe traversal or shadow-root traversal is implemented.
See `NOTES.md` for design details and limitations.
