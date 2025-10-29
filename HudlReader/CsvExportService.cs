using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace HudlReader;

public class CsvExportService
{
    public CsvExportService()
    {
    }

    public async Task Write(IReadOnlyList<InStatSnapshot> inStatSnapshots, string csvOutputPath)
    {
        await using StreamWriter writer = new StreamWriter(csvOutputPath);
        await using CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CsvExportMap>();
        await csv.WriteRecordsAsync(inStatSnapshots);
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