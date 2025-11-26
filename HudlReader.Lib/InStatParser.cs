namespace HudlReader.Lib;

public class InStatParser(string inputDirectory, string csvOutputDirectory, Action<int>? progress = null)
{
    private readonly Action<int>? _progressAction = progress;
    
    public async Task ParsePlayerReports()
    {
        // Parse all instat snapshot files in a directory
        // This assumes a single player and may fail if multiple players combined
        
        List<InStatSnapshot> inStatList = [];
        string[] pdfFiles = Directory.GetFiles(inputDirectory, "*.pdf");
        foreach (string file in pdfFiles)
        {
            Console.WriteLine($"Parsing file: '{Path.GetFileName(file)}'");
            bool parsed = InStatSnapshot.TryParse(file, out InStatSnapshot? localInStatSnapshot);

            if (parsed)
            {
                inStatList.Add(localInStatSnapshot);
                Console.WriteLine($"File '{Path.GetFileName(file)}' completed successfully");
            }
            else
            {
                Console.WriteLine($"File '{Path.GetFileName(file)}' failed");
            }

            // Report progress if the action is set
            if (this._progressAction != null)
            {
                int precentDone = GetProgressPercentage(inStatList.Count, pdfFiles.Length);
                this._progressAction(precentDone);
            }
        }
        
        Console.WriteLine($"{pdfFiles.Length} files parsed");
        
        if (inStatList.Count != 0)
        {
            CsvExportService csvExportService = new();
            List<InStatSnapshot> sortedList = inStatList.OrderBy(x => x.ReportDate).ToList();
            await csvExportService.Write(sortedList, Path.Combine(csvOutputDirectory, "output.csv"));
        }
    }
    
    private static int GetProgressPercentage(int current, int total) => (int)((double)current / total * 100);
}