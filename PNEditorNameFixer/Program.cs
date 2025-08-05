// See https://aka.ms/new-console-template for more information

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
var filesWithInnerTextNoForeOrSurname = SelectFilesWithoutForeOrSurname(hasEditorOrAuthor);
foreach (var file in filesWithInnerTextNoForeOrSurname)
{
    Console.WriteLine($"{file.Key} was one of the problem files");
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
            Console.WriteLine("The 'bibl' node does not have an 'author' child node.");
        }

    }
    
    return FilesWithoutForeOrSurname;
}

bool FilesLacksSurnameOrForename(XmlNode surname, XmlNode forename, string path)
{
    Console.WriteLine($"testing: {path}");
    if (surname != null && forename != null)
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