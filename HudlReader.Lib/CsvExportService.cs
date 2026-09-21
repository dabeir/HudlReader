using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;

namespace HudlReader.Lib;

public class CsvExportService
{
    public CsvExportService()
    {
    }

    public async Task Write(IReadOnlyList<InStatSnapshot> inStatSnapshots, string csvOutputPath)
    {
        await using (StreamWriter writer = new StreamWriter(csvOutputPath))
        await using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<CsvExportMap>();
            await csv.WriteRecordsAsync(inStatSnapshots);
        }

        await WriteDashboardDataFile(csvOutputPath);
    }

    // Dashboard.html can't auto-load output.csv via fetch() when opened directly from disk
    // (browsers block file:// fetch of local files), so mirror the CSV into a plain <script>
    // that Dashboard.html can include instead - script tags aren't subject to that restriction.
    private static async Task WriteDashboardDataFile(string csvOutputPath)
    {
        string csvText = await File.ReadAllTextAsync(csvOutputPath);
        string? directory = Path.GetDirectoryName(csvOutputPath);
        string dataFilePath = Path.Combine(directory ?? string.Empty, "dashboard-data.js");

        var payload = new
        {
            csv = csvText,
            generatedAt = DateTimeOffset.Now.ToString("o")
        };

        string jsContent = $"window.HUDL_READER_DASHBOARD_DATA = {JsonSerializer.Serialize(payload)};";
        await File.WriteAllTextAsync(dataFilePath, jsContent);
    }
}

internal sealed class CsvExportMap : ClassMap<InStatSnapshot>
{
    public CsvExportMap()
    {
        this.AutoMap(CultureInfo.InvariantCulture);
        // this.Map(p => p.HudlReport).Ignore();
        // this.Map(p => p.HudlReport.ReportName);
        // this.Map(p => p.HudlReport.ReportDate);
        // this.Map(p => p.HudlReport.TeamName);
        // this.Map(p => p.HudlReport.PlayerName);
    }
}