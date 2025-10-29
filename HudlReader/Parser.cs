namespace HudlReader;

public class Parser(string inputDirectory)
{
    public async Task Parse()
    {
        // Parse all instat snapshot files in a directory
        // This assumes a single player and may fail if multiple players combined
        
        List<InStatSnapshot> inStatList = new();
        string[] pdfFiles = Directory.GetFiles(inputDirectory, "*.pdf");
        foreach (string file in pdfFiles)
        {
            Console.WriteLine($"Parsing file: '{Path.GetFileName(file)}'");
            bool parsed = InStatSnapshot.TryParse(file, out InStatSnapshot? inStatSnapshot2);

            if (parsed)
            {
                inStatList.Add(inStatSnapshot2);
                Console.WriteLine($"File '{Path.GetFileName(file)}' completed successfully");
            }
            else
            {
                Console.WriteLine($"File '{Path.GetFileName(file)}' failed");
            }
        }
        
        Console.WriteLine($"{pdfFiles.Length} files parsed");
        
        if (inStatList.Any())
        {
            CsvExportService csvExportService = new();
            List<InStatSnapshot> sortedList = inStatList.OrderBy(x => x.ReportDate).ToList();
            await csvExportService.Write(sortedList, "C:\\Users\\MattB\\source\\prototype\\HudlReader\\CsvOutput\\instat_export.csv");
        }
    }
}