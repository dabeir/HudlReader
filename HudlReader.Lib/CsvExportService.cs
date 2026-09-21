using System.Globalization;
using System.Reflection;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;

namespace HudlReader.Lib;

public class CsvExportService
{
    private const string DashboardResourceFileName = "Dashboard.html";
    private const string DashboardDataPlaceholder = "<!--HUDL_READER_DASHBOARD_DATA-->";

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

        await WriteDashboardWithEmbeddedData(csvOutputPath);
    }

    // Dashboard.html can't auto-load output.csv via fetch() when opened directly from disk
    // (browsers block file:// fetch of local files), so embed the CSV directly into a copy of
    // the dashboard template instead - a plain inline <script> isn't subject to that restriction.
    private static async Task WriteDashboardWithEmbeddedData(string csvOutputPath)
    {
        string csvText = await File.ReadAllTextAsync(csvOutputPath);
        string? directory = Path.GetDirectoryName(csvOutputPath);
        string dashboardOutputPath = Path.Combine(directory ?? string.Empty, DashboardResourceFileName);

        string template = await ReadEmbeddedDashboardTemplate();

        var payload = new
        {
            csv = csvText,
            generatedAt = DateTimeOffset.Now.ToString("o")
        };

        // Guard against the (unlikely) case of "</script" appearing in report/team/player names,
        // which would otherwise prematurely close the <script> tag this gets embedded into.
        string json = JsonSerializer.Serialize(payload)
            .Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase);

        string dataScript = $"<script id=\"dashboardDataScript\">window.HUDL_READER_DASHBOARD_DATA = {json};</script>";
        string finalHtml = template.Replace(DashboardDataPlaceholder, dataScript);

        await File.WriteAllTextAsync(dashboardOutputPath, finalHtml);
    }

    private static async Task<string> ReadEmbeddedDashboardTemplate()
    {
        Assembly assembly = typeof(CsvExportService).GetTypeInfo().Assembly;
        string resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(DashboardResourceFileName));

        await using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using StreamReader reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
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