using CommandLine;
using OSIsoft.AF;
using OSIsoft.AF.PI;
using PILibrary;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ListAttributes
{
    class Program
    {
        class Options
        {
            [Option('o', "databaseOutputFile", Required = true, HelpText = "Database output file")]
            public string DatabaseOutputFile { get; set; }

            [Option('a', "attributeOutputFile", Required = true, HelpText = "Attribute output file")]
            public string AttributeOutputFile { get; set; }

            [Option('l', "logFile", Required = true, HelpText = "Log file")]
            public string LogFile { get; set; }

            [Option('x', "serverName", Required = true, HelpText = "PI Data Archive server name")]
            public string ServerName { get; set; }

            [Option('y', "systemName", Required = true, HelpText = "PI Asset Framework server name")]
            public string SystemName { get; set; }

            [Option('d', "databaseName", HelpText = "PI AF database name (optional, default is to list all databases)")]
            public string DatabaseName { get; set; }

            [Option('p', "numParallelTasks", Default = 20, HelpText = "Maximum number of simultaneous RPC calls")]
            public int NumParallelTasks { get; set; }

        }

        static void Main(string[] args)
        {

            Options arguments = Parser.Default.ParseArguments<Options>(args).MapResult(options => options, _ => null);
            Logger logger = new Logger(true, arguments.LogFile != null ? arguments.LogFile : null);

            logger.Log("Started");

            // Server

            logger.Log($"Connecting to server {arguments.ServerName}");
            PIServer piServer = PIUtil.GetPIServer(arguments.ServerName);

            logger.Log($"Connecting to system {arguments.SystemName}");
            PISystem piSystem = AFUtil.GetPISystem(arguments.SystemName);

            logger.Log("Connected");

            logger.Log($"Listing databases: system {arguments.SystemName}");
            AFUtil.WriteDatabases(piSystem, arguments.DatabaseOutputFile);

            // Database

            if (arguments.DatabaseName != null)
            {
                AFDatabase database = AFUtil.GetDatabase(piSystem, arguments.DatabaseName);

                logger.Log($"Listing attributes for database {database.Name}");
                AFUtil.WriteAttributes(database, arguments.AttributeOutputFile);
            }
            else
            {
                var outputFileExtension = Path.GetExtension(arguments.AttributeOutputFile);
                var outputFileName = Path.ChangeExtension(arguments.AttributeOutputFile, null);

                AFDatabases databases = piSystem.Databases;
                Parallel.ForEach(
                    databases,
                    new ParallelOptions { MaxDegreeOfParallelism = arguments.NumParallelTasks },
                    (AFDatabase database) =>
                    {
                        logger.Log($"Listing attributes: database {database.Name}");
                        AFUtil.WriteAttributes(database, $"{outputFileName} {database.Name}{outputFileExtension}");
                    });
            }
            
            logger.Log("Finished - press Enter to close terminal");
            logger.Close();
            Console.ReadLine();
        }
    }
}
