// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using DefaultNamespace;
using PNCheckNewXMLs;
using System.CommandLine;
using System.IO.Compression;

class PNEditorNameFixer
{
    private static Logger logger = null;
    public static void Main(string[] args)
    {
        logger = new Logger();
        Console.WriteLine("Logger creating, handling arguments");
        logger.Log("Logger creating, handling arguments");
        
         var helpOption = new Option<bool>(
                name: "--menu",
                description: "Show help menu.",
                getDefaultValue: () => false
            );
            helpOption.AddAlias("-h");

            var startYearOption = new Option<string>(
                name: "--start",
                description: "Show help menu.",
                getDefaultValue: () => "1"
            );
            helpOption.AddAlias("-sf");
            
            var endYearOption = new Option<string>(
                name: "--end",
                description: "Show help menu.",
                getDefaultValue: () => "98"
            );
            helpOption.AddAlias("-ef");
            
            // Create the root command for the application
            var rootCommand =
                new RootCommand(
                    "Name checker, runs against files to see if the author and editor names are properly formated.")
                {
                    helpOption,
                    startYearOption,
                    endYearOption
                };


            // Set the handler for the root command. This action will be executed when the command is invoked.
            rootCommand.SetHandler((context) =>
            {
                var showHelp = context.ParseResult.GetValueForOption(helpOption);
                if (showHelp)
                {
                    if (rootCommand.Description != null) context.Console.Out.Write(rootCommand.Description+ "\n");
                    context.ExitCode = 0;
                    rootCommand.Invoke("-h"); // force internal help logic
                    Environment.Exit(context.ExitCode);
                }

                // Retrieve the parsed values for each option
                var startYear = context.ParseResult.GetValueForOption(startYearOption) ?? "1";
                var endYear = context.ParseResult.GetValueForOption(endYearOption) ?? "98";

                logger.Log("Parsing args completed.");
                Console.WriteLine($"Args parsed. Start Year: {startYear}, End Year: {endYear}.");
                logger.Log($"Start Year: {startYear}, End Year: {endYear}");

                
                Core(startYear, endYear);
                
                /***
                
                Console.WriteLine("Have you pulled the latest version of IDP_DATA? (y/n)");
                var input = Console.ReadLine().ToLower();
                if (input == "y")
                {
                    // If all validations pass, proceed with the core application logic
                }
                else
                {
                    Console.WriteLine("If you don't have the most up to date info, this program should not run. " +
                                      "Please git pull for the newest info and then run me.");
                }*/
            });


            // Invoke the command line parser with the provided arguments
            // System.CommandLine will automatically handle help (-h or --help) and validation errors.
            rootCommand.Invoke(args);
    }

    static void Core(string start = "1", string end ="98")
    {
        Console.WriteLine("Working to Find biblio Directory");
        logger.Log("Working to fidn biblio");

        var directory = FindBiblioDirectory(logger);
        Console.WriteLine("Found biblio directory, creating file gatherer");
        logger.Log("Found biblio directory, creating file gatherer");

        var fileGatherer = new XMLEntryGatherer(directory, logger, start, end);
        Console.WriteLine("Created entry gatherer, starting to gather files");
        logger.Log("Created entry gatherer, starting to gather files");

        var filesFromBiblio = fileGatherer.GatherFiles();
        Console.WriteLine($"{filesFromBiblio.Count()} Files gathered, finding files with Editor or Author node");
        logger.Log("Files gathered, finding files with Editor or Author node");

        var hasEditorOrAuthor = SelectFilesWithEditorOrAuthor(filesFromBiblio);
        Console.WriteLine($"Found {hasEditorOrAuthor.Count} files with editor or author.\n previously found files, finding those without surname or forename child nodes");

        logger.Log("From previously found files, finding those without surname or forename child nodes");
        var filesWithoutForeOrSurname = SelectFilesWithoutForeOrSurname(hasEditorOrAuthor);
        Console.WriteLine($"Found {filesWithoutForeOrSurname.Count} files without forename or surname on the author node");
        UIUpdater(filesWithoutForeOrSurname);
    }

    static void UIUpdater(Dictionary<string, XmlDocument> xmlDocuments)
    {

        foreach (var file in xmlDocuments)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(file.Value.NameTable);
            nsmgr.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

            // Select the <surname> node directly under <author> using the namespace prefix
          //  string xPathAuthorQuery = "/*[local-name()='bibl']/*[local-name()='author']";
          //  string xPathEditorQuery = "/*[local-name()='bibl']/*[local-name()='editor']";
          string xPathAuthorQuery = "/tei:bibl/tei:author";
          string xPathEditorQuery = "/tei:bibl/tei:editor";
          var authorNodes = file.Value.SelectNodes(xPathAuthorQuery, nsmgr);
            var editorNodes = file.Value.SelectNodes(xPathEditorQuery, nsmgr);

            if (authorNodes != null && authorNodes.Count > 0)
            {
                for(int i = 0; i < authorNodes.Count; i++)
                {
                    var node = authorNodes[i];
                    if(CheckNodes(node, nsmgr, file.Key)) 
                        ParsePartsAndSave(node, file.Key, file.Value);
                }
            }else if (editorNodes != null && editorNodes.Count > 0)
            {
                for (int i = 0; i < editorNodes.Count; i++)
                {
                    var node = editorNodes[i];
                    if(CheckNodes(node, nsmgr, file.Key)) 
                        ParsePartsAndSave(node, file.Key, file.Value);
                }
            }
            
        }
    }

    static bool CheckNodes(XmlNode? node,XmlNamespaceManager nsmgr, string path)
    {
        if (node != null)
        {
            var surname = node.SelectSingleNode("/tei:surname", nsmgr);
            var forename = node.SelectSingleNode("/tei:forename", nsmgr);
            if (NodeLacksSurnameOrForename(surname, forename, path))
            {
                return true;
            }
            else
            {
                Console.WriteLine("How did we get a thing with no value?");
                return false;
            }
        }

        return false;
    }

    static void ParsePartsAndSave(XmlNode? node, string path, XmlDocument document)
    {
        
        var splitParts = node?.InnerText.Split(" ");
        var tempParts = splitParts;

        var parts = new List<string>();
        
        foreach (var part in tempParts)
        {
            if (part.Contains("."))
            {
                var split = part.Split(".");
                foreach(var dotPart in split) if(!string.IsNullOrEmpty(dotPart)) parts.Add(dotPart+".");
            }
            else
            {
                parts.Add(part);
            }
        }
        
            if (parts?.Count == 2)
            {
                var forename = document.CreateElement("forename", "http://www.tei-c.org/ns/1.0");
                forename.InnerText = parts[0];

                // Insert as first child of root element

                var surname = document.CreateElement("surname", "http://www.tei-c.org/ns/1.0");
                surname.InnerText = parts[1];

                if (node != null)
                {
                    ConfirmAndSaveNode(node, forename, surname, path, document);
                }
            }
            else if (parts?.Count == 1)
            {
                Console.WriteLine($"File {path} has one name: {parts[0]}, assuming its a surname and updating as requested");
                var surname = document.CreateElement("surname", "http://www.tei-c.org/ns/1.0");
                surname.InnerText = parts[0];

                if (node != null)
                {
                    ConfirmAndSaveNode(node, null, surname, path, document);
                }
            }
            else if (parts?.Count > 2)
            {
                Console.WriteLine($"File {path} has many names, asking user for input");
                var versions = GenerateVersionsOfName(parts.ToArray());
                var version = SelectVersion(versions);

                var forename = document.CreateElement("forename", "http://www.tei-c.org/ns/1.0");
                forename.InnerText = version.Forename;

                // Insert as first child of root element

                var surname = document.CreateElement("surname", "http://www.tei-c.org/ns/1.0");
                surname.InnerText = version.Surname;


                if (node != null)
                {
                    ConfirmAndSaveNode(node, forename, surname, path, document);
                }
            }
            else
            {
                Console.WriteLine(
                    $"Have to figure out the UI for this still, but there are more than two names in file: {path}, {node?.InnerText ?? "None"}  ({parts?.Count})");
                logger.LogProcessingInfo(
                    $"Have to figure out the UI for this still, but there are more than two names in file: {path}, {node?.InnerText ?? "None"}  ({parts?.Count})");
            }
    }

    static void ConfirmAndSaveNode(XmlNode? node, XmlNode? forename, XmlNode? surname, string fileName, XmlDocument file)
    {
        if (node != null)
        {
            node.InnerText = "";
            if (forename != null) node?.AppendChild(forename);
            if (surname != null) node?.AppendChild(surname);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Going to add <forename>{forename?.InnerText ?? ""}</forename> <surname>{surname?.InnerText ?? ""}</surname> to file @ {fileName}.\nPress y to save.");
            Console.ResetColor();
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Y)
            {
                file.Save(fileName);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(
                    $"Updated file at @ {fileName} to add values: [Forename: {forename?.InnerText ?? "None"}] [Surname: {surname?.InnerText ?? "None"}].");
                Console.ResetColor();
                logger.LogProcessingInfo(
                    $"Updated file at @ {fileName} to add forename and surname nodes with values: {forename?.InnerText ?? "None"}, {surname?.InnerText ?? "None"}.");
            }
            else
            {
                Console.WriteLine("Did not update file.");
                logger.LogProcessingInfo("did not update file");
            }
        }

    }

    static void PrintText(int number, string Forename, string Surname)
    {

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

    static (string Forename, string Surname) SelectVersion(List<(string Forename, string Surname)> versions)
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
            if (choice?.ToLower() == "0")
            {
                if (ConfirmChoice(("[NONE]", "[NONE]"))) return ("[NONE]", "[NONE]");
            }
            else if (choice != null && number.Match(choice).Success)
            {
                if (Int32.TryParse(choice, out var numb))
                {
                    if (numb > versions.Count)
                    {
                        Console.WriteLine($"Error, number {numb + 1} was outside of range 0-{versions.Count}");
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

    static bool ConfirmChoice((string, string) choice)
    {
        Console.WriteLine($"You selected {choice}. Press (y) if that is correct.");
        var key = Console.ReadKey();
        if (key.Key == ConsoleKey.Y) return true;
        else Console.WriteLine("Something other than Y was hit.");
        return false;
    }

    static List<(string Forename, string Surname)> GenerateVersionsOfName(string[] parts)
    {

        var versions = new List<(string Forename, string Surname)>();
        for (int i = 0; i < parts.Length; i++)
        {
            var version = GeneratePossibleVersions(parts, i);
            versions.Add(version);
        }

        return versions;
    }


    static (string Forename, string Surname) GeneratePossibleVersions(string[] parts, int forenameStart)
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


        foreName = foreName.Trim();
        lastName = lastName.Trim();


        return (foreName, lastName);
    }

    private static bool CheckFileForSurnameForename(string path, XmlDocument file)
    {
        logger.LogProcessingInfo($"Checking file: {path}");
        if (file?.NameTable != null)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(file.NameTable);
            nsmgr.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

            // Select the <surname> node directly under <author> using the namespace prefix
            //      string xPathAuthorQuery = "/*[local-name()='bibl']/*[local-name()='author']";
//        string xPathEditorQuery = "/*[local-name()='bibl']/*[local-name()='editor']";
            string xPathAuthorQuery = "/tei:bibl/tei:author";
            string xPathEditorQuery = "/tei:bibl/tei:editor";
            XmlNodeList? authorNodes = file.SelectNodes(xPathAuthorQuery, nsmgr);
            XmlNodeList? editorNodes = file.SelectNodes(xPathEditorQuery, nsmgr);

            if (authorNodes != null && authorNodes.Count > 0)
            {
                var surname = file.SelectNodes("/tei:bibl/tei:author/tei:forename", nsmgr);
                var forename = file.SelectNodes("/tei:bibl/tei:author/tei:surname", nsmgr);
                if (FilesLacksSurnameOrForename(surname, forename, path))
                {

                    Console.ForegroundColor = ConsoleColor.Green;
                    logger.LogProcessingInfo(
                        $"The file {path} lacks surname or forname, but has author node: {authorNodes[0]?.InnerText}");
                    Console.WriteLine($"The file {path} lacks surname or forname, but has author node");
                    Console.ResetColor();
                    return true;
                }
                else
                {
                    return false;
                    //Console.ForegroundColor = ConsoleColor.Red;
                    //Console.WriteLine($"File {xmlDocument.Key}: [{forename?.InnerText ?? "No Forename"}] [{surname?.InnerText ?? "No Surname"}] ");
                    // Console.WriteLine($"The file {xmlDocument.Key} has either surname or forname");
                    //Console.ResetColor();
                }
            }
            else if (editorNodes != null && editorNodes.Count > 0)
            {
                //Console.WriteLine($"File {xmlDocument.Key} had an editor node");
                var forename = file?.SelectNodes("/tei:bibl/tei:editor/tei:forename", nsmgr);
                var surname = file?.SelectNodes("/tei:bibl/tei:editor/tei:surname", nsmgr);

                if (FilesLacksSurnameOrForename(surname, forename, path))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    logger.LogProcessingInfo(
                        $"The file {path} lacks surname or forname but has editor node with value: {editorNodes[0]?.InnerText}");
                    Console.WriteLine($"The file {path} lacks surname or forname but has editor node");
                    Console.ResetColor();
                    if (file != null) return true;
                }
                else
                {
                    //Console.WriteLine($"File {xmlDocument.Key} had both fore and surname.");
                    //Console.ForegroundColor = ConsoleColor.Red;
                    //Console.WriteLine($"File {xmlDocument.Key}: [{forename?.InnerText ?? "No Forename"}] [{surname?.InnerText ?? "No Surname"}] ");
                    //logger.LogProcessingInfo($"File {xmlDocument.Key}: [{forename?.InnerText ?? "No Forename"}] [{surname?.InnerText ?? "No Surname"}] ");
                    //Console.ResetColor();
                    return false;
                }
            }
            else
            {
                if (authorNodes.Count == 0 && editorNodes.Count == 0)
                    Console.WriteLine(
                        $"The 'bibl' {path} node does not have an 'author' or 'editor' child node.");
                else
                    Console.WriteLine(
                        $"The file {path} does not have subnodes for surname and forename it seems.");
                logger.LogProcessingInfo(
                    $"The 'bibl' {path} node does not have an 'author' or 'editor' child node.");
            }
        }

        return false;
    }

//var filesWithoutAnyUndernodes = SelectFilesWithNoUndernodes(filesWithInnerTextNoForeOrSurname);

    static Dictionary<string, XmlDocument> SelectFilesWithoutForeOrSurname(Dictionary<string, XmlDocument> xmlDocuments)
    {
        var FilesWithoutForeOrSurname = new Dictionary<string, XmlDocument>();

        logger.LogProcessingInfo($"Checking {xmlDocuments.Count} files to see if they have fore or surname");
        foreach (var xmlDocument in xmlDocuments)
        {
            if (CheckFileForSurnameForename(xmlDocument.Key, xmlDocument.Value))
            {
                FilesWithoutForeOrSurname.Add(xmlDocument.Key, xmlDocument.Value);
            }
        }


        foreach (var file in FilesWithoutForeOrSurname)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(file.Value.NameTable);
            nsmgr.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

            string xPathAuthorQuery = "/tei:bibl/tei:author";
            string xPathEditorQuery = "/tei:bibl/tei:editor";
            var author = file.Value.SelectNodes(xPathAuthorQuery, nsmgr);
            var editor = file.Value.SelectNodes(xPathEditorQuery, nsmgr);

            var text = "";
            
            if(author != null && author.Count > 0) text = author[0].InnerText;
            if(editor != null && editor.Count > 0) text = editor[0].InnerText;

            Console.WriteLine($"File: {file.Key}, {text}");
        }

        return FilesWithoutForeOrSurname;
    }

    static bool NodeLacksSurnameOrForename(XmlNode? surname, XmlNode? forename, string path)
    {
            logger.LogProcessingInfo($"testing: {path}");
            if ((surname != null) || ( forename != null))
            {
                //Console.ForegroundColor = ConsoleColor.Green;
            
                logger.LogProcessingInfo(
                    $"Found author node with fore and surname nodes in file {path}, with values: [{surname?.InnerText ?? "None"}] [{forename?.InnerText ?? "None"}]");
                //Console.ResetColor();
                return false;
            }
            else
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                logger.LogProcessingInfo($"Could not find either author fore or surname nodes in file {path}");
                //Console.ResetColor();
                return true;
            }
    }

    static bool FilesLacksSurnameOrForename(XmlNodeList? surname, XmlNodeList? forename, string path)
    {
        logger.LogProcessingInfo($"testing: {path}");
        if ((surname != null && surname.Count > 0) || ( forename != null && forename.Count > 0))
        {
            //Console.ForegroundColor = ConsoleColor.Green;
            var surnameText = "";
            var forenameText = "";

            for (int i = 0; i < forename?.Count; i++)
            {
                forenameText += forename[i]?.InnerText;
            }
            
            for (int i = 0; i < surname?.Count; i++)
            {
                surnameText += surname[i]?.InnerText;
            }
            
            logger.LogProcessingInfo(
                $"Found author node with fore and surname nodes in file {path}, with values: [{surnameText ?? "None"}] [{forenameText ?? "None"}]");
            //Console.ResetColor();
            return false;
        }
        else
        {
            //Console.ForegroundColor = ConsoleColor.Red;
            logger.LogProcessingInfo($"Could not find either author fore or surname nodes in file {path}");
            //Console.ResetColor();
            return true;
        }
    }

    static Dictionary<string, XmlDocument> SelectFilesWithEditorOrAuthor(Dictionary<string, XmlDocument> xmlDocuments)
    {
        var DocsWithEditorOrAuthor = new Dictionary<string, XmlDocument>();

        Console.WriteLine("Selecting files");
        logger.LogProcessingInfo("Selecting files");
        foreach (var xmlDocument in xmlDocuments)
        {

            Console.WriteLine($"Checking: {xmlDocument.Key}");
            logger.LogProcessingInfo($"Checking: {xmlDocument.Key}");
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.Value.NameTable);
            nsmgr.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

            // Select the <surname> node directly under <author> using the namespace prefix
            XmlNode? authorfirstName = xmlDocument.Value.SelectSingleNode("//tei:bibl/tei:author", nsmgr);
            XmlNode? editorFirstName = xmlDocument.Value.SelectSingleNode("//tei:bibl/tei:editor", nsmgr);

            if (authorfirstName != null || editorFirstName != null
                && (authorfirstName?.ParentNode?.InnerXml.Contains("bibl") ?? false)
                && (editorFirstName?.ParentNode?.InnerXml.Contains("bibl") ?? false))

                if (authorfirstName != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Found author for file: {xmlDocument.Key}");
                }

            if (editorFirstName != null)
            {
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Found editor for file: {xmlDocument.Key}");
                }
                logger.LogProcessingInfo($"Found author for file: {xmlDocument.Key}");
                Console.ResetColor();
                DocsWithEditorOrAuthor.Add(xmlDocument.Key, xmlDocument.Value);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Could not find author for file: {xmlDocument.Key}");
                logger.LogProcessingInfo($"Could not find author for file: {xmlDocument.Key}");
                Console.ResetColor();
            }
        }

        return DocsWithEditorOrAuthor;
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
}