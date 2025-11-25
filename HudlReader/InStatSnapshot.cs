using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace HudlReader;

public class InStatSnapshot(
    string reportName,
    DateTime reportDate,
    string teamName,
    string opponentName,
    string playerName,
    int playerJerseyNumber,
    int inStatIndex,
    int goals,
    int assists,
    int points,
    int plusMinus,
    TimeSpan timeOnIce,
    int shifts,
    TimeSpan averageShift,
    TimeSpan powerPlayTime,
    TimeSpan shortHandedTime,
    TimeSpan penaltyMinutes,
    int shots,
    int shotsOnGoal,
    int shotsOnGoalPercentage,
    int powerPlayShots,
    int powerPlayShotsOnGoal,
    int powerPlayShotsOnGoalPercentage,
    int corsi,
    int corsiPlus,
    int corsiMinus,
    int hitsDelivered,
    int hitsAgainst,
    int blockedShots,
    decimal xGoals,
    decimal xGoalsPerShot,
    decimal xGoalsPerGoal,
    decimal teamXGoalsWhenOnIce,
    decimal opponentsXGoalsWhenOnIce)
{

    public string ReportName { get; } = reportName;
    public DateTime ReportDate { get; } = reportDate;
    public string TeamName { get; } = teamName;
    public string OpponentName { get; } = opponentName;
    public string PlayerName { get; } = playerName;
    public int PlayerJerseyNumber { get; } = playerJerseyNumber;
    public int InStatIndex { get; } = inStatIndex;
    public int Goals { get; } = goals;
    public int Assists { get; } = assists;
    public int Points { get; } = points;
    public int PlusMinus { get; } = plusMinus;
    public TimeSpan TimeOnIce { get; } = timeOnIce;
    public int Shifts { get; } = shifts;
    public TimeSpan AverageShift { get; } = averageShift;
    public TimeSpan PowerPlayTime { get; } = powerPlayTime;
    public TimeSpan ShortHandedTime { get; } = shortHandedTime;
    public TimeSpan PenaltyMinutes { get; } = penaltyMinutes;
    public int Shots { get; } = shots;
    public int ShotsOnGoal { get; } = shotsOnGoal;
    public int ShotsOnGoalPercentage { get; } = shotsOnGoalPercentage;
    public int PowerPlayShots { get; } = powerPlayShots;
    public int PowerPlayShotsOnGoal { get; } = powerPlayShotsOnGoal;
    public int PowerPlayShotsOnGoalPercentage { get; } = powerPlayShotsOnGoalPercentage;
    public int Corsi { get; } = corsi;
    public int CorsiPlus { get; } = corsiPlus;
    public int CorsiMinus { get; } = corsiMinus;
    public int HitsDelivered { get; } = hitsDelivered;
    public int HitsAgainst { get; } = hitsAgainst;
    public int BlockedShots { get; } = blockedShots;
    public decimal XGoals { get; } = xGoals;
    public decimal XGoalsShots { get; } = xGoalsPerShot;
    public decimal XGoalsGoals { get; } = xGoalsPerGoal;
    public decimal TeamXGoalsWhenOnIce { get; } = teamXGoalsWhenOnIce;
    public decimal OpponentsXGoalsWhenOnIce { get; } = opponentsXGoalsWhenOnIce;

    public static bool TryParse(string pdfFile, out InStatSnapshot? inStatSnapshot)
    {
        try
        {
            HudlReport.TryParse(pdfFile, out HudlReport? hudlReport);

            // Parse Page 1 of the InStat PDF
            using PdfDocument document = PdfDocument.Open(pdfFile);
            Page page1 = document.GetPage(1);
            string pageOneText = ContentOrderTextExtractor.GetText(page1);
            string[] splitLines = pageOneText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            string reportName = splitLines[1];
            // Parse the date using a custom date time format
            DateTime reportDate = DateTime.ParseExact(splitLines[2], "dd.MM.yyyy", CultureInfo.InvariantCulture);
            string teamName = splitLines[3];

            // Opponent name
            string opponentName = reportName.Split(':').FirstOrDefault(s => !s.Contains(teamName)) ?? string.Empty;

            // Parse page 2 of the InStat PDF
            Page page2 = document.GetPage(2);
            string pageTwoText = ContentOrderTextExtractor.GetText(page2);

            int index = ParseInStatIndex(pageTwoText);
            int goals = ParseIntValue(pageTwoText, @"Goals\s+([—\-]|\d+\.?\d*)");
            int assists = ParseIntValue(pageTwoText, @"Assists\s+([—\-]|\d+\.?\d*)");
            int points = ParseIntValue(pageTwoText, @"Points\s+([—\-]|\d+\.?\d*)");
            int plusMinus = ParsePlusMinus(pageTwoText);
            TimeSpan timeOnIce = ParseTimeValue(pageTwoText, @"Time on ice\s+(\d{1,2}:\d{2})");
            int shifts = ParseIntValue(pageTwoText, @"Shifts\s+(\d+)");
            TimeSpan averageShift = ParseTimeValue(pageTwoText, @"Average shift duration\s+(\d{1,2}:\d{2})");
            TimeSpan powerPlayTime = ParseTimeValue(pageTwoText, @"Power play time\s+(\d{1,2}:\d{2})");
            TimeSpan shortHandedTime = ParseTimeValue(pageTwoText, @"Short-handed time\s+(\d{1,2}:\d{2})");
            TimeSpan penaltyMinutes = ParsePenaltyTime(pageTwoText);

            (int playerJerseyNumber, string playerName) = ParsePlayerInfo(pageTwoText);
            (int shots, int onGoal, int onGoalPercentage) = ParseShotsOnGoal(pageTwoText);
            (int powerPlayShots, int powerPlayShotsOnGoal, int powerPlayShotsOnGoalPercentage) =
                ParsePowerPlayShotsOnGoal(pageTwoText);

            int corsi = ParseIntValue(pageTwoText, @"CORSI\s+(\d+)");
            int corsiPlus = ParseIntValue(pageTwoText, @"CORSI\+\s+(\d+)");
            int corsiMinus = ParseIntValue(pageTwoText, @"CORSI-\s+(\d+)");
            int hitsDelivered = ParseIntValue(pageTwoText, @"Hits\s+([—\-]|\d+\.?\d*)");
            int hitsAgainst = ParseIntValue(pageTwoText, @"Hits against\s+([—\-]|\d+\.?\d*)");
            int blockedShots = ParseIntValue(pageTwoText, @"Blocked shots\s+(\d+\.?\d*)");

            // Expected goals
            decimal xGoals = ParseDecimalValue(pageTwoText, @"xG\s+(\d+\.?\d*)");
            decimal xGoalsShots = ParseDecimalValue(pageTwoText, @"xG / shots\s+([—\-]|\d+\.?\d*)");
            decimal xGoalsGoals = ParseDecimalValue(pageTwoText, @"xG / goals\s+([—\-]|\d+\.?\d*)");
            decimal teamXGoalsWhenOnIce = ParseDecimalValue(pageTwoText, @"Team xG when on ice\s+(\d+\.?\d*)");
            decimal opponentsXGoalsWhenOnIce =
                ParseDecimalValue(pageTwoText, @"Opponent's xG when on ice\s+(\d+\.?\d*)");

            inStatSnapshot = new InStatSnapshot(
                reportName,
                reportDate,
                teamName,
                opponentName,
                playerName,
                playerJerseyNumber,
                index,
                goals,
                assists,
                points,
                plusMinus,
                timeOnIce,
                shifts,
                averageShift,
                powerPlayTime,
                shortHandedTime,
                penaltyMinutes,
                shots,
                onGoal,
                onGoalPercentage,
                powerPlayShots,
                powerPlayShotsOnGoal,
                powerPlayShotsOnGoalPercentage,
                corsi,
                corsiPlus,
                corsiMinus,
                hitsDelivered,
                hitsAgainst,
                blockedShots,
                xGoals,
                xGoalsShots,
                xGoalsGoals,
                teamXGoalsWhenOnIce,
                opponentsXGoalsWhenOnIce
            );

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error parsing InStat snapshot: {e.Message}");
            inStatSnapshot = null;
            return false;
        }
    }

    private static (int number, string name) ParsePlayerInfo(string text)
    {
        // Split by lines and find the line before "Season average"
        string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length - 1; i++)
        {
            if (lines[i + 1].Trim().StartsWith("Season average", StringComparison.OrdinalIgnoreCase))
            {
                // Found "Season average", check the previous line
                string playerLine = lines[i].Trim();
                string pattern = @"^(\d+)\s+(.+)$";
                Match match = Regex.Match(playerLine, pattern);

                if (match.Success)
                {
                    int number = int.Parse(match.Groups[1].Value);
                    string name = match.Groups[2].Value.Trim();
                    return (number, name);
                }
            }
        }

        return (0, string.Empty);
    }

    // Helper method to parse InStat Index (first value)
    private static int ParseInStatIndex(string text)
    {
        string pattern = @"InStat Index\s+(\d+)";
        Match match = Regex.Match(text, pattern);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    // Helper method to parse integer values (first column value)
    private static int ParseIntValue(string text, string pattern)
    {
        return (int)Math.Round(ParseDecimalValue(text, pattern));
    }

    // Helper method to parse decimal values
    private static decimal ParseDecimalValue(string text, string pattern)
    {
        Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string value = match.Groups[1].Value;
            // Handle dashes (no value)
            if (value == "—" || value == "-")
            {
                return 0;
            }

            // Handle decimal values
            return decimal.Parse(value);
        }

        return 0;
    }

    // Helper method to parse Plus Minus (can be negative, first value)
    private static int ParsePlusMinus(string text)
    {
        string pattern = @"Plus Minus\s+(-?\d+\.?\d*)";
        Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return (int)Math.Round(decimal.Parse(match.Groups[1].Value));
        }

        return 0;
    }

    // Helper method to parse time values (first column value)
    private static TimeSpan ParseTimeValue(string text, string pattern)
    {
        Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string[] timeParts = match.Groups[1].Value.Split(':');
            return new TimeSpan(0, int.Parse(timeParts[0]), int.Parse(timeParts[1]));
        }

        return TimeSpan.Zero;
    }

    // Helper method to parse penalty time (handles dash in first column)
    private static TimeSpan ParsePenaltyTime(string text)
    {
        string pattern = @"Penalty time\s+([—\-]|\d{1,2}:\d{2})";
        Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string value = match.Groups[1].Value;
            // Handle dash (no penalty time)
            if (value == "—" || value == "-")
            {
                return TimeSpan.Zero;
            }

            string[] timeParts = value.Split(':');
            return new TimeSpan(0, int.Parse(timeParts[0]), int.Parse(timeParts[1]));
        }

        return TimeSpan.Zero;
    }

    // Helper method to parse shots on goal with 3 distinct values
    // Example: "Shots / on goal 5/3 60% 1.5/1 67%" returns (5, 3, 60)
    private static (int shots, int onGoal, int percentage) ParseShotsOnGoal(string text)
    {
        string pattern = @"Shots / on goal\s+(\d+)/(\d+)\s+(\d+)%";
        Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success)
        {
            int shots = int.Parse(match.Groups[1].Value);
            int onGoal = int.Parse(match.Groups[2].Value);
            int percentage = int.Parse(match.Groups[3].Value);
            return (shots, onGoal, percentage);
        }

        return (0, 0, 0);
    }

    // Helper method to parse shots on goal with 3 distinct values
    // Example: "Shots / on goal 5/3 60% 1.5/1 67%" returns (5, 3, 60)
    private static (int shots, int onGoal, int percentage) ParsePowerPlayShotsOnGoal(string text)
    {
        string pattern = @"Power play shots\s+(\d+)\s*/\s*(\d+)\s+(\d+)%";
        Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success)
        {
            int shots = int.Parse(match.Groups[1].Value);
            int onGoal = int.Parse(match.Groups[2].Value);
            int percentage = int.Parse(match.Groups[3].Value);
            return (shots, onGoal, percentage);
        }

        return (0, 0, 0);
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        sb.AppendLine($"Report Name: {this.ReportName}");
        sb.AppendLine($"Report Date: {this.ReportDate:d}");
        sb.AppendLine($"Team Name: {this.TeamName}");
        sb.AppendLine($"Opponent Name: {this.OpponentName}");
        sb.AppendLine($"Player Name: {this.PlayerName}");
        sb.AppendLine($"Player Number: {this.PlayerJerseyNumber}");
        sb.AppendLine($"InStat Index: {this.InStatIndex}");
        sb.AppendLine($"Goals: {this.Goals}");
        sb.AppendLine($"Assists: {this.Assists}");
        sb.AppendLine($"Points: {this.Points}");
        sb.AppendLine($"Plus/Minus: {this.PlusMinus}");
        sb.AppendLine($"Time on Ice: {this.TimeOnIce:mm\\:ss}");
        sb.AppendLine($"Shifts: {this.Shifts}");
        sb.AppendLine($"Average Shift: {this.AverageShift:mm\\:ss}");
        sb.AppendLine($"Power Play Time: {this.PowerPlayTime:mm\\:ss}");
        sb.AppendLine($"Short-Handed Time: {this.ShortHandedTime:mm\\:ss}");
        sb.AppendLine($"Penalty Minutes: {this.PenaltyMinutes:mm\\:ss}");
        sb.AppendLine($"Shots: {this.Shots}");
        sb.AppendLine($"Shots On Goal: {this.ShotsOnGoal}");
        sb.AppendLine($"Shots On Goal Percentage: {this.ShotsOnGoalPercentage}%");
        sb.AppendLine($"Power Play Shots: {this.PowerPlayShots}");
        sb.AppendLine($"Power Play Shots On Goal: {this.PowerPlayShotsOnGoal}");
        sb.AppendLine($"Power Play Shots On Goal Percentage: {this.PowerPlayShotsOnGoalPercentage}%");
        sb.AppendLine($"Corsi: {this.Corsi}");
        sb.AppendLine($"CorsiPlus: {this.CorsiPlus}");
        sb.AppendLine($"CorsiMinus: {this.CorsiMinus}");
        sb.AppendLine($"Hits Delivered: {this.HitsDelivered}");
        sb.AppendLine($"Hits Against: {this.HitsAgainst}");
        sb.AppendLine($"Blocked Shots: {this.BlockedShots}");
        sb.AppendLine($"xG: {this.XGoals}");
        sb.AppendLine($"xG / shots: {this.XGoalsShots}");
        sb.AppendLine($"xG / goals: {this.XGoalsGoals}");
        sb.AppendLine($"Team xG when on ice: {this.TeamXGoalsWhenOnIce}");
        sb.AppendLine($"Opponent's xG when on ice: {this.OpponentsXGoalsWhenOnIce}");

        return sb.ToString();
    }
}