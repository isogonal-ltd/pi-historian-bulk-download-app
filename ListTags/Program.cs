using CommandLine;
using OSIsoft.AF;
using OSIsoft.AF.PI;
using PILibrary;
using System;
using System.Threading.Tasks;

namespace ListAttributesAndTags
{
    class Program
    {
        class Options
        {
            [Option('t', "tagOutputFile", Required = true, HelpText = "Tag output file")]
            public string TagOutputFile { get; set; }
            
            [Option('l', "logFile", Required = true, HelpText = "Log file")]
            public string LogFile { get; set; }

            [Option('x', "serverName", Required = true, HelpText = "PI Data Archive server name")]
            public string ServerName { get; set; }

            [Option('y', "systemName", Required = true, HelpText = "PI Asset Framework server name")]
            public string SystemName { get; set; }
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
            
            // Database

            logger.Log($"Listing tags");
            PIUtil.WriteTags(piServer, arguments.TagOutputFile);

            logger.Log("Finished - press Enter to close terminal");
            logger.Close();
            Console.ReadLine();
        }
    }
}
