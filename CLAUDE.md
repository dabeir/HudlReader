# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution Overview

A .NET 10.0 solution (`HudlReader.sln`) that parses InStat/Hudl hockey player-performance PDF reports and turns them into a CSV file plus a browsable HTML dashboard. It consists of three projects:

- **`HudlReader.Lib`** — Shared library: PDF parsing (`InStatParser`, `InStatSnapshot`), CSV export (`CsvExportService`), and the embedded `Dashboard.html` resource.
- **`HudlReader.Cli`** — Console app (`System.CommandLine`). Takes `--in`/`--out` directory arguments and drives `InStatParser` headlessly.
- **`HudlReader.UI`** — Avalonia desktop GUI (Windows). Folder-picker front end over the same `InStatParser`/`CsvExportService` pipeline, with a progress bar.

There are no automated tests in this repository.

## Build & Run

```bash
# Build the entire solution
dotnet build HudlReader.sln

# Run the CLI
dotnet run --project HudlReader.Cli -- --in "C:\hudlreports" --out "C:\csvoutput"

# Run the desktop GUI
dotnet run --project HudlReader.UI

# Publish the CLI as a self-contained single-file Windows x64 executable
# (SelfContained/PublishSingleFile/RuntimeIdentifier are already baked into HudlReader.Cli.csproj)
dotnet publish HudlReader.Cli/HudlReader.Cli.csproj -c Release
```

.NET 10 runtime is required to run the published output; see `HudlReader.Cli/README.txt` for the end-user instructions that ship alongside the CLI executable.

## Git Workflow

No explicit workflow notes yet — infer from the user's usual conventions unless told otherwise (don't run `git commit`/`git push` unless explicitly asked).

## Architecture

### Data Flow

1. `InStatParser.ParsePlayerReports()` (Lib) enumerates every `*.pdf` in the input directory.
2. Each file is handed to `InStatSnapshot.TryParse`, which opens it with **PdfPig**, extracts page 1 (report name, date, team) and page 2 (all stat fields) as plain text via `ContentOrderTextExtractor`, and pulls individual values out with per-field regexes (goals, assists, time-on-ice, shots/on-goal splits, Corsi, expected-goals, etc.).
3. Successfully parsed snapshots are collected into a `List<InStatSnapshot>`, sorted by `ReportDate`, and written out in one shot by `CsvExportService.Write` (CsvHelper, auto-mapped, one row per report/game).
4. Both front ends (`Cli`, `UI`) call the exact same `InStatParser`/`CsvExportService` pipeline — the only difference is how the input/output directories are supplied and how progress/errors are surfaced.
5. The user opens the generated `output.csv` in the embedded `Dashboard.html` (a standalone Chart.js + PapaParse page) in a browser to visualize trends across games.

### PDF Parsing Conventions (`InStatSnapshot.cs`)

- **Parsing is single-player, single-report only.** `TryParse` assumes exactly one player's stats per PDF; a report combining multiple players will silently mis-parse or throw (caught in `InStatParser`, logged, and skipped — that file is just dropped from the CSV).
- **All stat fields are pulled by regex over the plain-text page dump**, not by PDF layout/coordinates. Field order in the PDF doesn't matter, but the exact label text ("Time on ice", "Power play time", "CORSI+", etc.) does — a wording change in a future InStat export format will silently return `0`/`TimeSpan.Zero` for that field rather than erroring.
- **En-dash/hyphen placeholders (`—` / `-`) mean "no value" and are parsed as `0`**, not skipped or null — see `ParseDecimalValue`/`ParsePenaltyTime`. Don't assume a `0` in the output CSV means the stat was literally zero; it may mean InStat reported no value for that game.
- **Compound fields are parsed together, not per-column.** `ParseShotsOnGoal`/`ParsePowerPlayShotsOnGoal` match a single regex against strings like `"Shots / on goal 5/3 60%"` and return a 3-tuple; there's no way to independently re-parse just the percentage.
- **Player name/number come from a positional heuristic**, not a labeled field: `ParsePlayerInfo` scans for the line immediately before `"Season average"` and regex-matches `^(\d+)\s+(.+)$` against it. If InStat ever reorders page 2, this is the field most likely to start returning `(0, "")`.
- **Opponent name is derived, not printed directly** — `TryParse` splits the page-1 report-name line on `:` and takes whichever side isn't the known team name.

### CSV Export (`CsvExportService.cs`)

- Uses CsvHelper's `AutoMap` against every public property of `InStatSnapshot`, so **adding a public property to `InStatSnapshot` automatically adds a CSV column** — no export-side wiring needed (unlike MKAT's Excel exporter, there's no manual column-index list to keep in sync).
- `CsvExportMap` exists as the place to add `.Map(...)`/`.Ignore()` overrides later; currently it does nothing beyond the automap.
- Output is always a single file named `output.csv` in the chosen output directory, overwritten on every run — there's no per-run timestamping or append mode.

### CLI (`HudlReader.Cli`)

- Built on **`System.CommandLine`** (`2.0.0-rc.1...`, a prerelease API — expect breaking changes if bumping this package).
- Only two options exist: `--in` and `--out`, both plain strings with no validation beyond the `InStatParser` call being skipped if either is null/whitespace.
- Progress and errors are just `Console.WriteLine`; there's no exit-code contract yet (exceptions inside the action are logged and rethrown, but nothing sets `Environment.ExitCode`).

### Desktop UI (`HudlReader.UI`, Avalonia MVVM)

- Uses **CommunityToolkit.Mvvm** (`[ObservableProperty]`, `[RelayCommand]`) — same pattern as MKAT's `MainWindowViewModel`.
- `MainWindowViewModel.FuncPickFolderAsync` is a delegate injected from the code-behind (`MainWindow.xaml.cs`) so the ViewModel can trigger Avalonia's `IStorageProvider` folder picker without taking a direct dependency on `Window`/`StorageProvider`.
- `StartProcessing` copies the embedded `Dashboard.html` resource into the output folder (via `Assembly.GetManifestResourceStream`, matched by filename suffix) before kicking off parsing on a background `Task.Run`, so the user always has a fresh dashboard sitting next to `output.csv`.
- Progress updates come back through the same `Action<int>` callback the CLI ignores, marshaled onto the UI thread with `Dispatcher.UIThread.InvokeAsync`.
- There is currently no input validation UI — the `// TODO: Show validation message` in `StartProcessing` is the known gap when input/output folders aren't set.

### Dashboard (`HudlReader.Lib/Web/Dashboard.html`)

- Fully standalone HTML/CSS/JS — no build step, no bundler. Pulls **Chart.js 3.9.1** and **PapaParse 5.3.2** from cdnjs.
- Reads whatever `output.csv` the user drops in/uploads client-side (PapaParse) and renders stat cards/charts entirely in the browser; it has no dependency on the .NET projects at runtime beyond being handed a CSV in the expected column shape.
- Because CsvHelper auto-maps `InStatSnapshot`'s public properties in declaration order, **reordering or renaming properties on `InStatSnapshot` changes the CSV header row the dashboard expects** — check `Dashboard.html`'s column-name references before renaming a property.

### Packaging

- Only `HudlReader.Cli` is currently configured for self-contained single-file publishing (`PublishSingleFile`, `SelfContained`, `RuntimeIdentifier=win-x64`, embedded PDB) directly in its `.csproj` — no separate publish profile or installer script exists yet (unlike MKAT's Velopack setup).
- `HudlReader.UI` has no publish-specific MSBuild properties set yet; it currently builds as a normal framework-dependent app.

### Continuous Integration

No CI configuration (`.github/workflows/`) exists in this repository yet.
