
using HudlReader;

Console.WriteLine("HudlReader");

Parser parser = new("C:\\Users\\MattB\\source\\prototype\\HudlReader\\Sample");
await parser.Parse();

Console.WriteLine("Done");
