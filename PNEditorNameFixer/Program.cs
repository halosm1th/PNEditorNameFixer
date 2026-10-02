// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using DefaultNamespace;
using PNCheckNewXMLs;

var logger = new Logger();
Console.WriteLine("Logger creating, finding biblio directory");
logger.Log("Logger creating, finding biblio directory");

var directory = FindBiblioDirectory(logger);
Console.WriteLine("Found biblio directory, creating file gatherer");
logger.Log("Found biblio directory, creating file gatherer");

var fileGatherer = new XMLEntryGatherer(directory, logger);
Console.WriteLine("Created entry gatherer, starting to gather files");
logger.Log("Created entry gatherer, starting to gather files");

var filesFromBiblio  = fileGatherer.GatherFiles();
Console.WriteLine($"{filesFromBiblio.Count()} Files gathered, finding files with Editor or Author node");
logger.Log("Files gathered, finding files with Editor or Author node");

var needsWork = SelectFilesNeedingNameTagging(filesFromBiblio);
Console.WriteLine($"{needsWork.Count} files have untagged author/editor names");
UIUpdater(needsWork);

void UIUpdater(Dictionary<string, (XmlDocument Doc, List<XmlElement> Nodes)> work)
{
    foreach (var (path, (doc, nodes)) in work)
    {
        bool changed = false;

        foreach (var node in nodes)
        {
            var parts = node.InnerText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string? fore = null;
            string sur;

            if (parts.Length == 1)
            {
                sur = parts[0];
            }
            else if (parts.Length == 2)
            {
                fore = parts[0];
                sur = parts[1];
            }
            else
            {
                Console.WriteLine($"{path}: '{node.InnerText}' has many parts, asking user");
                var v = SelectVersion(GenerateVersionsOfName(parts));
                if (v.Surname == "[NONE]") continue;   // user skipped
                fore = string.IsNullOrEmpty(v.Forename) ? null : v.Forename;
                sur = v.Surname;
            }

            node.InnerText = "";
            if (fore != null)
            {
                var f = doc.CreateElement("forename", node.NamespaceURI);
                f.InnerText = fore;
                node.AppendChild(f);
            }
            var s = doc.CreateElement("surname", node.NamespaceURI);
            s.InnerText = sur;
            node.AppendChild(s);
            changed = true;

            logger.LogProcessingInfo($"{path}: set forename '{fore ?? "None"}', surname '{sur}'");
        }

        if (changed)
        {
            Console.WriteLine($"Going to update {path}. Press any key to continue.");
            Console.ReadKey();
            doc.Save(path);
            Console.WriteLine($"Updated {path}.");
        }
    }
}

void PrintText(int number, string Forename, string Surname){

    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.Write($"{number}) ");
    Console.ResetColor();
    Console.Write("Forename: ");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write($"{Forename}");
    Console.ResetColor();
    Console.Write(", Surname: ");
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.Write($"{Surname}.\n");
    Console.ResetColor();
}

(string Forename, string Surname) SelectVersion(List<(string Forename, string Surname)> versions)
{
    (string Forename, string Surname) version = ("[NONE]", "[NONE]");
    bool chosen = false;
    do
    {
        PrintText(0, version.Forename, version.Surname);
        for (int i = 1; i < versions.Count; i++)
        {
            var displayNumber = i;
            PrintText(displayNumber, versions[i].Forename, versions[i].Surname);
        }

        var choice = Console.ReadLine();
        var number = new Regex(@"\d+");
        if (choice.ToLower() == "0")
        {
            if(ConfirmChoice(("[NONE]", "[NONE]"))) return ("[NONE]", "[NONE]");
        }else if (number.Match(choice).Success)
        {
            if (Int32.TryParse(choice, out var numb))
            {
                if (numb >= versions.Count)
                {
                    Console.WriteLine($"Error, number {numb+1} was outside of range 0-{versions.Count}");
                }
                else
                {
                    version = versions[numb];
                    if (ConfirmChoice(version)) return version;
                    else version = ("[NONE]", "[NONE]");
                }
            }
        }
        else
        {
            Console.WriteLine($"Please enter a number between 0-{versions.Count}");
        }

    } while (!chosen);

    return version;
}

bool ConfirmChoice((string, string) choice)
{
    Console.WriteLine($"You selected {choice}. Press (y) if that is correct.");
    var key = Console.ReadKey();
    if(key.Key == ConsoleKey.Y) return true;
    else Console.WriteLine("Something other than Y was hit.");
    return false;
}

List<(string Forename, string Surname)> GenerateVersionsOfName(string[] parts)
{
    
    var versions = new List<(string Forename, string Surname)>();
    for (int i = 0; i < parts.Length; i++)
    {
        var version = GeneratePossibleVersions(parts, i);
        versions.Add(version);
    }

    return versions;
}


(string Forename, string Surname) GeneratePossibleVersions(string[] parts, int forenameStart)
{
    if (forenameStart > parts.Length) throw new ArgumentOutOfRangeException(nameof(forenameStart));
    
    string foreName = "";

    for (int i = 0; i < forenameStart; i++)
    {
        foreName += parts[i] + " ";
    }

    string lastName = "";

    for (int i = forenameStart; i < parts.Length; i++)
    {
        lastName = lastName + parts[i] + " ";
    }

    
    foreName= foreName.Trim();
    lastName = lastName.Trim();
    

    return (foreName, lastName);
}


void ProcessNode(XmlNodeList nodeWithName, KeyValuePair<string, XmlDocument> document)
{
    var authorNode = document.Value;
}

//var filesWithoutAnyUndernodes = SelectFilesWithNoUndernodes(filesWithInnerTextNoForeOrSurname);

static List<XmlElement> FindUntaggedNameNodes(XmlDocument doc)
{
    const string q = "/*[local-name()='bibl']/*[local-name()='author' or local-name()='editor']";
    var result = new List<XmlElement>();
    foreach (XmlElement node in doc.SelectNodes(q)!)
    {
        bool hasText = !string.IsNullOrWhiteSpace(node.InnerText);
        bool hasNameChildren = node.SelectSingleNode(
            "*[local-name()='forename' or local-name()='surname']") != null;
        if (hasText && !hasNameChildren) result.Add(node);
    }
    return result;
}

Dictionary<string, (XmlDocument Doc, List<XmlElement> Nodes)> SelectFilesNeedingNameTagging(
    Dictionary<string, XmlDocument> docs)
{
    var result = new Dictionary<string, (XmlDocument, List<XmlElement>)>();
    foreach (var (path, doc) in docs)
    {
        var nodes = FindUntaggedNameNodes(doc);
        if (nodes.Count > 0)
        {
            logger.LogProcessingInfo($"{path}: {nodes.Count} untagged author/editor node(s)");
            result[path] = (doc, nodes);
        }
    }
    return result;
}

static string FindBiblioDirectory(Logger logger)
{
    logger.LogProcessingInfo("Finding biblio directory");
    var directoryFinder = new XMLDirectoryFinder(logger);
    var startingDir = Directory.GetCurrentDirectory();
    logger.LogProcessingInfo($"Starting directory for search: {startingDir}");
    var directory = directoryFinder.FindBiblioDirectory(startingDir);
    logger.LogProcessingInfo($"Found biblio directory: {directory}");
    return directory;
}
