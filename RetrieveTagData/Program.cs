using CommandLine;
using OSIsoft.AF;
using OSIsoft.AF.PI;
using PILibrary;
using System;
using System.Collections.Generic;

namespace PIRecordedValuesQueryAsync
{
    class Program
    {

        class Options
        {
            [Option('i', "inputType", Required = true, HelpText = "RecordedTag, RecordedAttribute, InterpolatedTag, InterpolatedAttribute")]
            public string InputType { get; set; }

            [Option('f', "inputFile", Required = true, HelpText = "Input file")]
            public string InputFile { get; set; }

            [Option('o', "outputDirectory", Required = true, HelpText = "Output directory")]
            public string OutputDirectory { get; set; }

            [Option('l', "logFile", Required = true, HelpText = "Log file")]
            public string LogFile { get; set; }

            [Option('a', "serverName", Required = true, HelpText = "PI Data Archive server name")]
            public string ServerName { get; set; }

            [Option('b', "systemName", Required = true, HelpText = "PI Asset Framework server name")]
            public string SystemName { get; set; }

            [Option('d', "databaseName", Required = true, HelpText = "PI AF database name")]
            public string DatabaseName { get; set; }

            [Option('p', "numParallelTasks", Default = 10, HelpText = "Maximum number of simultaneous RPC calls")]
            public int NumParallelTasks { get; set; }

            [Option('t', "timeResolution", Default = "none", HelpText = "Output directory structure: split data by 'none', 'year', 'month', or 'day'")]
            public string TimeResolution { get; set; }

            [Option('y', "numYears", Default = 100, HelpText = "Output directory structure: number of years if timeResolution = 'year'")]
            public int NumYears { get; set; }

            [Option('s', "pageSize", Default = 10000, HelpText = "Maximum number of points per RPC call")]
            public int PageSize { get; set; }
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

            // Tags

            logger.Log($"Reading tags from {arguments.InputFile}");

            List<AbstractRetrievePoints> tagClasses = null;
            switch (arguments.InputType)
            {
                case "RecordedTag":
                    tagClasses = MainFunctions.LoadTagClassesRecorded(
                        arguments.InputFile,
                        arguments.OutputDirectory,
                        arguments.TimeResolution,
                        arguments.NumYears,
                        arguments.PageSize,
                        piServer,
                        piSystem,
                        arguments.NumParallelTasks,
                        logger);
                    break;

                case "InterpolatedTag":
                    tagClasses = MainFunctions.LoadTagClassesInterpolated(
                        arguments.InputFile,
                        arguments.OutputDirectory,
                        arguments.TimeResolution,
                        arguments.NumYears,
                        arguments.PageSize,
                        piServer,
                        piSystem,
                        arguments.NumParallelTasks,
                        logger);
                    break;

                case "RecordedAttribute":
                    tagClasses = MainFunctions.LoadAttributeClassesRecorded(
                        arguments.InputFile,
                        arguments.OutputDirectory,
                        arguments.TimeResolution,
                        arguments.NumYears,
                        arguments.PageSize,
                        piServer,
                        piSystem,
                        AFUtil.GetDatabase(piSystem, arguments.DatabaseName),
                        arguments.NumParallelTasks,
                        logger);
                    break;

                case "InterpolatedAttribute":
                    tagClasses = MainFunctions.LoadAttributeClassesInterpolated(
                        arguments.InputFile,
                        arguments.OutputDirectory,
                        arguments.TimeResolution,
                        arguments.NumYears,
                        arguments.PageSize,
                        piServer,
                        piSystem,
                        AFUtil.GetDatabase(piSystem, arguments.DatabaseName),
                        arguments.NumParallelTasks,
                        logger);
                    break;

                default:
                    break;
            }
            logger.Log($"Tags read");

            logger.Log($"Reading tag data from {arguments.ServerName}, numParallelTasks = {arguments.NumParallelTasks}");
            MainFunctions.DoStuffParallelForeach(tagClasses, arguments.NumParallelTasks, logger);
            logger.Log($"Data read");

            logger.Log("Finished - press Enter to close terminal");
            logger.Close();
            Console.ReadLine();
        }
    }
}
