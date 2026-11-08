# HudlReader

Turn a folder of **Hudl / InStat hockey player-performance PDF reports** into a single CSV file and an interactive, self-contained HTML dashboard that tracks a player's trends game over game.

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows)
![Avalonia](https://img.shields.io/badge/UI-Avalonia-8B44AC)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

<!-- Add a screenshot of the dashboard here, e.g.:
![Dashboard screenshot](docs/dashboard.png)
-->

---

## Why

InStat delivers one PDF per player per game. That's great for reviewing a single game, but it makes it hard to answer questions like *"Is my ice time going up?"* or *"How has my shot accuracy changed since October?"*.

HudlReader reads every report in a folder, pulls out the stats, and lines them all up in one place.

## Features

- **Batch PDF parsing.** Drop all your reports in one folder and parse them in one run.
- **CSV export.** One row per game, sorted by date, ready for Excel, Google Sheets or your own analysis.
- **Interactive dashboard.** A single `Dashboard.html` file with summary cards and charts:
  - InStat Index by game
  - Points (goals / assists)
  - Time on ice
  - Plus/minus trend
  - Shots vs. shots on goal
  - Power-play and short-handed time
  - Corsi events
  - Physical play (hits delivered, hits received, blocked shots)
  - Puck possession (recoveries, losses, battles). Hidden automatically if your reports don't include these stats.
- **Zero-setup viewing.** The data is embedded directly in the generated dashboard, so you can just double-click it. No web server, no upload step.
- **Dark and light themes**, with your choice remembered.
- **Static export.** Save a snapshot of the dashboard (charts baked in as images) to share with coaches or family.
- **Two ways to run it:** a desktop app with folder pickers and a progress bar, or a command-line tool for scripting.

## Stats captured

Every row in `output.csv` includes:

| Category | Fields |
|---|---|
| Report | Report name, date, team, opponent, player name, jersey number |
| Scoring | Goals, assists, points, plus/minus, InStat Index |
| Ice time | Time on ice, shifts, average shift length, power-play time, short-handed time, penalty minutes |
| Shooting | Shots, shots on goal, on-goal %, power-play shots / on goal / % |
| Advanced | Corsi, Corsi+, Corsi−, expected goals (xG), xG per shot, xG per goal, team xG and opponent xG while on ice |
| Physical | Hits delivered, hits against, blocked shots |
| Puck possession | Puck recoveries, puck losses, puck battles won / lost / win % |

> **Note:** When InStat shows a stat as "—" (no value), HudlReader records it as `0`. A `0` in the CSV can mean either "zero" or "not tracked for that game".

## Getting started

### Requirements

- Windows 10/11 (x64)
- [.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0) to run it, or the .NET 10 SDK to build from source
- A modern web browser to view the dashboard

### Option 1: Desktop app

```bash
dotnet run --project HudlReader.UI
```

1. Click **Browse…** next to **Input Folder** and pick the folder containing your Hudl PDF reports.
2. Click **Browse…** next to **Output Folder** and pick where the results should go.
3. (Optional) Tick **Open dashboard after parsing is complete**.
4. Click **Parse Data**.

### Option 2: Command line

```bash
dotnet run --project HudlReader.Cli -- --in "C:\hudlreports" --out "C:\csvoutput"
```

| Option | Description |
|---|---|
| `--in` | Folder containing the Hudl/InStat PDF reports (`*.pdf`) |
| `--out` | Folder where `output.csv` and `Dashboard.html` will be written |

### Output

Either way, the output folder ends up with two files:

```
csvoutput/
├── output.csv        # one row per game, every stat
└── Dashboard.html    # open in any browser; data is already loaded
```

Both files are overwritten on every run. When a new report comes in, add the PDF to your input folder, re-run HudlReader, and refresh the dashboard.

## Building from source

```bash
git clone https://github.com/dabeir/HudlReader.git
cd HudlReader
dotnet build HudlReader.slnx
```

### Publishing a standalone executable

```bash
# Command-line tool: single self-contained .exe (settings are in the .csproj)
dotnet publish HudlReader.Cli/HudlReader.Cli.csproj -c Release

# Desktop app: single-file .exe (requires the .NET 10 runtime on the target machine)
dotnet publish HudlReader.UI/HudlReader.UI.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Project structure

```
HudlReader/
├── HudlReader.Lib/        # Shared library: PDF parsing, CSV export, dashboard template
│   ├── InStatParser.cs    #   Walks the input folder and parses each PDF
│   ├── InStatSnapshot.cs  #   One game's stats, extracted from a single report
│   ├── CsvExportService.cs#   Writes output.csv and the data-embedded Dashboard.html
│   └── Web/Dashboard.html #   Standalone Chart.js dashboard (embedded resource)
├── HudlReader.Cli/        # Command-line front end (System.CommandLine)
└── HudlReader.UI/         # Desktop front end (Avalonia, MVVM)
```

### How it works

1. `InStatParser` finds every `*.pdf` in the input folder.
2. Each report is opened with [PdfPig](https://github.com/UglyToad/PdfPig). Page 1 provides the game info (date, teams) and page 2 provides the player's stats, which are extracted from the page text with regular expressions.
3. The parsed games are sorted by date and written to `output.csv` with [CsvHelper](https://joshclose.github.io/CsvHelper/).
4. The dashboard template is copied next to the CSV with the data embedded inline, so it works when opened straight from disk.

## Limitations

- **One player per report.** Each PDF is expected to contain a single player's stats. Files that can't be parsed are logged and skipped.
- **Tied to InStat's report layout.** Stats are found by their label text (e.g. "Time on ice", "CORSI+"). If InStat changes the wording in a future export, the affected fields will come through as `0` instead of failing loudly.
- **English reports only.** The parser matches the English labels (files ending in `(eng).pdf`).

## Built with

- [.NET 10](https://dotnet.microsoft.com/)
- [PdfPig](https://github.com/UglyToad/PdfPig) for PDF text extraction
- [CsvHelper](https://joshclose.github.io/CsvHelper/) for CSV writing
- [Avalonia UI](https://avaloniaui.net/) and [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) for the desktop app
- [System.CommandLine](https://github.com/dotnet/command-line-api) for the CLI
- [Chart.js](https://www.chartjs.org/) and [PapaParse](https://www.papaparse.com/) for the dashboard

## License

HudlReader is released under the [MIT License](LICENSE).

## Disclaimer

HudlReader is an independent, personal project. It is not affiliated with, endorsed by, or supported by Hudl or InStat. All trademarks belong to their respective owners.
