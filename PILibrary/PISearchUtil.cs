using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using OSIsoft.AF.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PILibrary
{
    public static class PIUtil
    {

        // PISystem level
        public static PIServer GetPIServer(string serverName)
        {
            PIServers piServers = new PIServers();
            return piServers[serverName];
        }

        // Tag level
        public static void WriteTags(PIServer piServer, string outputFile)
        {
            using (StreamWriter outputStreamWriter = new StreamWriter(outputFile))
            {
                foreach (PIPoint tag in PIPoint.FindPIPoints(piServer, "*"))
                {
                    string path = tag.GetPath();
                    //Console.WriteLine(path);
                    outputStreamWriter.WriteLine(path);
                }
            }
        }
    }

    public static class AFUtil
    {

        // PISystem level

        public static PISystem GetPISystem(string serverName)
        {
            PISystems piSystems = new PISystems();
            return piSystems[serverName];
        }

        public static void WriteDatabases(PISystem piSystem, string outputFile)
        {
            using (StreamWriter outputStreamWriter = new StreamWriter(outputFile))
            {
                AFDatabases databases = piSystem.Databases;
                foreach (AFDatabase database in databases)
                {
                    outputStreamWriter.WriteLine(database.Name);
                }
            }
        }

        // Database level

        public static AFDatabase GetDatabase(PISystem piSystem, string databaseName)
        {
            return piSystem.Databases[databaseName];
        }

        public static void WriteAttributes(AFDatabase database, string outputFile)
        {
            using (StreamWriter outputStreamWriter = new StreamWriter(outputFile))
            {
                AFElements elements = database.Elements;
                foreach (AFElement element in elements)
                {
                    ListAttributes(element, outputStreamWriter);
                    ListElements(element, outputStreamWriter);
                }
            }
        }

        private static void ListElements(AFElement parentElement, StreamWriter outputFile)
        {
            AFElements elements = parentElement.Elements;
            foreach (AFElement element in elements)
            {
                ListAttributes(element, outputFile);
                ListElements(element, outputFile);
            }
        }

        private static void ListAttributes(AFElement element, StreamWriter outputFile)
        {
            AFAttributes attributes = element.Attributes;
            foreach (AFAttribute attribute in attributes)
            {
                String tagName;
                try
                {
                    tagName = attribute.PIPoint.Name;
                }
                catch (Exception) {
                    tagName = "NONE";
                }
                outputFile.WriteLine(attribute.GetPath() + "," + tagName);
            }
        }

        // Attribute level

        public static AFAttribute FindAttribute(AFDatabase database, string attributePath)
        {
            AFAttribute attribute = AFAttribute.FindAttribute(attributePath, database);
            try
            {
                PIPoint tag = attribute.PIPoint;
                return attribute;
            }
            catch
            {
                Console.WriteLine($"{attributePath} PIPoint could not be found, returning null");
                return null;
            }
        }

        public static List<AFElement> FindAllElements(AFDatabase database)
        {
            AFElementSearch search = new AFElementSearch(database, "mySearch", "*");
            return search.FindElements(fullLoad: true).ToList();
        }
    }
}
