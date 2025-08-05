// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using DefaultNamespace;
using PNCheckNewXMLs;

var logger = new Logger();
Console.WriteLine("Logger creating, finding biblio directory");
var directory = FindBiblioDirectory(logger);
Console.WriteLine("Found biblio directory, creating file gatherer");

var fileGatherer = new XMLEntryGatherer(directory, logger);
Console.WriteLine("Created entry gatherer, starting to gather files");
var filesFromBiblio  = fileGatherer.GatherFiles();
Console.WriteLine("Files gathered, finding files with Editor or Author node");

var hasEditorOrAuthor = SelectFilesWithEditorOrAuthor(filesFromBiblio);
Console.WriteLine("From previously found files, finding those without surname or forename child nodes");
var filesWithoutForeOrSurname = SelectFilesWithoutForeOrSurname(hasEditorOrAuthor);
UIUpdater(filesWithoutForeOrSurname);

void UIUpdater(Dictionary<string, XmlDocument> xmlDocuments)
{
    
    foreach (var file in xmlDocuments)
    {
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(file.Value.NameTable);
        nsmgr.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

        // Select the <surname> node directly under <author> using the namespace prefix
        string xPathAuthorQuery = "/*[local-name()='bibl']/*[local-name()='author']";
        string xPathEditorQuery = "/*[local-name()='bibl']/*[local-name()='editor']";
        var authorNodes = file.Value.SelectSingleNode(xPathAuthorQuery);
        var editorNodes = file.Value.SelectSingleNode(xPathEditorQuery);
        var parts = authorNodes.InnerText.Split(" ");
        if (parts.Length == 2)
        {
            var forename = file.Value.CreateElement("forename", "http://www.tei-c.org/ns/1.0");
            forename.InnerText = parts[0];

            // Insert as first child of root element

            var surname = file.Value.CreateElement("surname", "http://www.tei-c.org/ns/1.0");
            surname.InnerText = parts[1];

            if (authorNodes != null)
            {
                authorNodes.InnerText = "";
                authorNodes?.AppendChild(forename);
                authorNodes?.AppendChild(surname);
                Console.WriteLine($"Going to update file @ {file.Key}. Press any key to");
                Console.ReadKey();
                file.Value.Save(file.Key);
                Console.WriteLine($"Updated file at @ {file.Key}.");
            }
            else if (editorNodes != null)
            {
                editorNodes.InnerText = "";
                editorNodes?.AppendChild(forename);
                editorNodes?.AppendChild(surname);
                Console.WriteLine($"Going to update file @ {file.Key}. Press any key to");
                Console.ReadKey();
                file.Value.Save(file.Key);
                Console.WriteLine($"Updated file at @ {file.Key}.");

            }
        }else if (parts.Length == 1)
        {
            Console.WriteLine($"File {file.Key} has one name, assuming its a surname and updating as requested");
            var surname = file.Value.CreateElement("surname", "http://www.tei-c.org/ns/1.0");
            surname.InnerText = parts[0];

            if (authorNodes != null)
            {
                authorNodes.InnerText = "";
                authorNodes?.AppendChild(surname);
                Console.WriteLine($"Going to update file @ {file.Key}. Press any key to");
                Console.ReadKey();
                file.Value.Save(file.Key);
                Console.WriteLine($"Updated file at @ {file.Key}.");
            }
            else if (editorNodes != null)
            {
                editorNodes.InnerText = "";
                editorNodes?.AppendChild(surname);
                Console.WriteLine($"Going to update file @ {file.Key}. Press any key to");
                Console.ReadKey();
                file.Value.Save(file.Key);
                Console.WriteLine($"Updated file at @ {file.Key}.");
            }
        } else if (parts.Length > 2)
        {
            Console.WriteLine($"File {file.Key} has many names, asking user for input");
            var versions = GenerateVersionsOfName(parts);
            var version = SelectVersion(versions);

            var forename = file.Value.CreateElement("forename", "http://www.tei-c.org/ns/1.0");
            forename.InnerText = version.Forename;

            // Insert as first child of root element

            var surname = file.Value.CreateElement("surname", "http://www.tei-c.org/ns/1.0");
            surname.InnerText = version.Surname;

            if (authorNodes != null)
            {
                authorNodes.InnerText = "";
                authorNodes?.AppendChild(forename);
                authorNodes?.AppendChild(surname);
                Console.WriteLine($"Going to update file @ {file.Key}. Press any key to");
                Console.ReadKey();
                file.Value.Save(file.Key);
                Console.WriteLine($"Updated file at @ {file.Key}.");
            }
            else if (editorNodes != null)
            {
                editorNodes.InnerText = "";
                editorNodes?.AppendChild(forename);
                editorNodes?.AppendChild(surname);
                Console.WriteLine($"Going to update file @ {file.Key}. Press any key to");
                Console.ReadKey();
                file.Value.Save(file.Key);
                Console.WriteLine($"Updated file at @ {file.Key}.");

            }
        }
        else
        {
            Console.WriteLine($"Have to figure out the UI for this still, but there are more than two names in file: {file.Key}, {authorNodes.InnerText}  ({parts.Length})");
        }
    }
}

(string Forename, string Surname) SelectVersion(List<(string Forename, string Surname)> versions)
{
    (string Forename, string Surname) version = ("[NONE]", "[NONE]");
    bool chosen = false;
    do
    {
        Console.WriteLine($"0) Forename: {version.Forename}, Surname: {version.Surname}.");
        for (int i = 1; i < versions.Count; i++)
        {
            var displayNumber = i;
            Console.WriteLine($"{displayNumber}) Forename: {versions[i].Forename}, Surname: {versions[i].Surname}.");
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
                if (numb > versions.Count)
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

Dictionary<string, XmlDocument> SelectFilesWithoutForeOrSurname(Dictionary<string, XmlDocument> xmlDocuments)
{    
    var FilesWithoutForeOrSurname = new Dictionary<string, XmlDocument>();

    foreach (var xmlDocument in xmlDocuments)
    {
        
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.Value.NameTable);
        nsmgr.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

        // Select the <surname> node directly under <author> using the namespace prefix
        string xPathAuthorQuery = "/*[local-name()='bibl']/*[local-name()='author']";
        string xPathEditorQuery = "/*[local-name()='bibl']/*[local-name()='editor']";
        XmlNodeList authorNodes = xmlDocument.Value.SelectNodes(xPathAuthorQuery);
        XmlNodeList editorNodes = xmlDocument.Value.SelectNodes(xPathEditorQuery);
        if (authorNodes != null && authorNodes.Count > 0)
        {
            XmlNode surname = xmlDocument.Value.SelectSingleNode("//tei:bibl/tei:author/tei:forename", nsmgr);
            XmlNode forename = xmlDocument.Value.SelectSingleNode("//tei:bibl/tei:author/tei:surname", nsmgr);
            if (FilesLacksSurnameOrForename(surname, forename, xmlDocument.Key))
            {
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"The file {xmlDocument.Key} lacks surname or forname, but has author node");
                Console.ResetColor();
                FilesWithoutForeOrSurname.Add(xmlDocument.Key, xmlDocument.Value);
            }
            else
            {
               // Console.ForegroundColor = ConsoleColor.Red;
               // Console.WriteLine($"The file {xmlDocument.Key} has either surname or forname");
               // Console.ResetColor();
            }
        }else if (editorNodes != null && editorNodes.Count > 0)
        {
            XmlNode forename = xmlDocument.Value.SelectSingleNode("//tei:bibl/tei:editor/tei:forename", nsmgr);
            XmlNode surname = xmlDocument.Value.SelectSingleNode("//tei:bibl/tei:editor/tei:surname", nsmgr);

            if (FilesLacksSurnameOrForename(surname, forename, xmlDocument.Key))
            {
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"The file {xmlDocument.Key} lacks surname or forname but has editor node");
                Console.ResetColor();
                FilesWithoutForeOrSurname.Add(xmlDocument.Key, xmlDocument.Value);
            }
            else
            {
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"The file {xmlDocument.Key} has either surname or forname");
                Console.ResetColor();
            }
        }
        else
        {
            Console.WriteLine($"The 'bibl' {xmlDocument.Key} node does not have an 'author' or 'editor' child node.");
        }

    }
    
    return FilesWithoutForeOrSurname;
}

bool FilesLacksSurnameOrForename(XmlNode surname, XmlNode forename, string path)
{
    //Console.WriteLine($"testing: {path}");
    if (surname != null || forename != null)
    {
        //Console.ForegroundColor = ConsoleColor.Green;
        //Console.WriteLine($"Found author node with fore and surname nodes in file {path}, with values: {surname.InnerText} {forename.InnerText}");
        //Console.ResetColor();
        return false;
    }
    else
    {
        //Console.ForegroundColor = ConsoleColor.Red;
        //Console.WriteLine($"Could not find either author fore or surname nodes in file {path}"); 
        //Console.ResetColor();
        return true;
    }
}

Dictionary<string, XmlDocument> SelectFilesWithEditorOrAuthor(Dictionary<string, XmlDocument> xmlDocuments)
{
    var DocsWithEditorOrAuthor = new Dictionary<string, XmlDocument>();

    Console.WriteLine("Selecting files");
    foreach (var xmlDocument in xmlDocuments)
    {

        Console.WriteLine($"Checking: {xmlDocument.Key}");
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.Value.NameTable);
        nsmgr.AddNamespace("tei", "http://www.tei-c.org/ns/1.0");

        // Select the <surname> node directly under <author> using the namespace prefix
        XmlNode authorfirstName = xmlDocument.Value.SelectSingleNode("//tei:bibl/tei:author", nsmgr);
        XmlNode editorFirstName = xmlDocument.Value.SelectSingleNode("//tei:bibl/tei:editor", nsmgr);

        if (authorfirstName != null || editorFirstName != null 
            && (authorfirstName?.ParentNode.InnerXml.Contains("bibl") ?? false)
            && (editorFirstName? .ParentNode.InnerXml.Contains("bibl") ?? false))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Found author for file: {xmlDocument.Key}");
            Console.ResetColor();
            DocsWithEditorOrAuthor.Add(xmlDocument.Key, xmlDocument.Value);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Could not find author for file: {xmlDocument.Key}");
            Console.ResetColor();
        }
    }
    
    return DocsWithEditorOrAuthor;
}


static string FindBiblioDirectory(Logger logger)
{
    var directoryFinder = new XMLDirectoryFinder(logger);
    var startingDir = Directory.GetCurrentDirectory();
    var directory = directoryFinder.FindBiblioDirectory(startingDir);
    return directory;
}