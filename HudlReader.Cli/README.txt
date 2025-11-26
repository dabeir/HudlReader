README
-------------

IMPORTANT:
.NET 9 Runtime is required run this application and can be downloaded from here:
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-9.0.307-windows-x64-installer

How to execute:
HudlReader.Cli.exe --in "path to folder containing all hudl reports" --out "path to csv output folder"

Example: 
HudlReader.Cli.exe --in "C:\hudlreports" --out "c:\csvoutput"

So, create a folder to hold all of your hudl reports and another folder to hold the output csv file.

Then, run the HudlReader using the path to the newly created folders as the arguments as shown in the example above.

After executing you should see a file named "output.csv" in the CSV output folder.

Once you've created an "output.csv" file open it with the provided "Dashboard.html" file (Note you will need to open the dashboard.html in a browser first).

Any time you receive a new hudl report just re-run the "HudlReader.Cli.exe" application and refresh the "Dashboard.html" file in your browser.