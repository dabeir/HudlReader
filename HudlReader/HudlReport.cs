using System.Globalization;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace HudlReader;

public class HudlReport(string reportName, DateTime reportDate, string teamName, string playerName)
{
    public string ReportName { get; } = reportName;
    public DateTime ReportDate { get; } = reportDate;
    public string TeamName { get;} = teamName;
    public string PlayerName { get; } = playerName;
    
    public static bool TryParse(string pdfFile, out HudlReport? hudlReport)
    {
        try
        {
            using PdfDocument document = PdfDocument.Open(pdfFile);
            Page page = document.GetPage(1);
            string text = ContentOrderTextExtractor.GetText(page);
            string[] splitLines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            // Parse the date using a custom date time format
            DateTime reportDate = DateTime.ParseExact(splitLines[2], "dd.MM.yyyy", CultureInfo.InvariantCulture);
        
            // Extra only the player's name
            int dottedLineIndex = splitLines[4].IndexOf('.');
            string playerName = splitLines[4].Substring(0, dottedLineIndex);
        
            hudlReport = new HudlReport(splitLines[1], reportDate, 
                splitLines[3], playerName);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        hudlReport = null;
        return false;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        
        sb.AppendLine($"Report Name: {this.ReportName}");
        sb.AppendLine($"Report Date: {this.ReportDate:d}");
        sb.AppendLine($"Team Name: {this.TeamName}");
        sb.AppendLine($"Player Name: {this.PlayerName}");

        return sb.ToString();
    }
}