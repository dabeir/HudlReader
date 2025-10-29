
using System.CommandLine;
using HudlReader;

Console.WriteLine("HudlReader");

Option<string> inputDirectoryOption = new Option<string>("--in")
{
    Description = "Input directory",
    // DefaultValueFactory = parseResult => ""
};

Option<string> outputDirectoryOption = new Option<string>("--out")
{
    Description = "CSV output directory",
    // DefaultValueFactory = parseResult => ""
};

RootCommand rootCommand = new RootCommand("Enter command line parameters for the HudlReader");
rootCommand.Options.Add(inputDirectoryOption);
rootCommand.Options.Add(outputDirectoryOption);

// Handle input params
rootCommand.SetAction(async parseResult =>
{
    try
    {
        string? inputDirectory = parseResult.GetValue(inputDirectoryOption);
        Console.WriteLine($"Input: {inputDirectory}");
        
        string? outputDirectory = parseResult.GetValue(outputDirectoryOption);
        Console.WriteLine($"Output: {outputDirectory}");

        if (!string.IsNullOrWhiteSpace(inputDirectory) && !string.IsNullOrWhiteSpace(outputDirectory))
        {
            Parser parser = new(inputDirectory, outputDirectory);
            await parser.Parse();
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
});

// Parse all input params
ParseResult rootResult = rootCommand.Parse(args);

// Execute the app once all params have been parsed
await rootResult.InvokeAsync();

Console.WriteLine("Done");
