using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using System;
using System.IO;
using System.Linq;

namespace PILibrary
{

    public static class AFQueryUtil
    {
        public static AFValue GetNextRecordedValue(PIPoint tag, AFTime startTime, int lookAhead = 100)
        {
            AFValues values = tag.RecordedValuesByCount(startTime, lookAhead, forward: true, boundaryType: AFBoundaryType.Inside, filterExpression: null, includeFilteredValues: false);
            return values.OrderBy(value => value.Timestamp).ToList()[0];
        }

    }

    public static class PIRandomFunctionsUtil
    {
        
        public enum TimeResolution
        {
            None,
            Year,
            Month,
            Day
        }

        public static TimeResolution ParseTimeResolutionString(string timeResolutionString)
        {
            switch (timeResolutionString)
            {
                case "none":
                    return TimeResolution.None;
                case "year":
                    return TimeResolution.Year;
                case "month":
                    return TimeResolution.Month;
                case "day":
                    return TimeResolution.Day;
                default:
                    throw new Exception("Time resolution string must be one of \"year\", \"month\", or \"day\"");
            }
        }

        public static string DateTimeToDatePath(DateTime time, TimeResolution timeResolution, int numYears = 1)
        {
            switch (timeResolution)
            {
                case TimeResolution.None:
                    {
                        return "";
                    }
                case TimeResolution.Year:
                    {
                        int year = time.Year - (time.Year % numYears);
                        return $"{year}";
                    }
                case TimeResolution.Month:
                    {
                        return time.ToString("yyyy/MM");
                    }
                case TimeResolution.Day:
                    {
                        return time.ToString("yyyy/MM/dd");
                    }
                default:
                    throw new Exception("Time resolution wasn't found");
            }
        }

    }

    public class Logger
    {

        bool console;
        StreamWriter logWriter = null;

        public Logger(bool console = true, string logPath = null)
        {
            this.console = console;
            if (logPath != null)
            {
                Directory.CreateDirectory(new FileInfo(logPath).Directory.FullName);
                this.logWriter = new StreamWriter(logPath);
            }
        }
        
        public void Log<T>(T obj)
        {
            string timeStamp = DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss:ffff]");
            Console.WriteLine($"{timeStamp} {obj}");
            if (this.logWriter != null)
            {
                logWriter.WriteLine($"{timeStamp} {obj}");
                logWriter.Flush();
            }
        }

        public void Close()
        {
            if (this.logWriter != null)
            {
                this.logWriter.Close();
            }
        }

    }

}
