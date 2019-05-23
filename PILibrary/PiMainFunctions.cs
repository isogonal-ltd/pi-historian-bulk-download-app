using OSIsoft.AF;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OSIsoft.AF.Asset;

// https://pisquare.osisoft.com/thread/8111
// https://technodocbox.com/Java/69845166-Best-practices-for-building-af-sdk-applications.html

namespace PILibrary
{
    public class MainFunctions
    {
        public static List<String[]> LoadInputFile(String inputFilePath)
        {
            var inputLines = new List<String[]>();
            using (var inputStreamReader = new StreamReader(inputFilePath))
            {
                while (!inputStreamReader.EndOfStream)
                {
                    inputLines.Add(inputStreamReader.ReadLine().Split(','));
                }
            }
            return inputLines;
        }


        public static List<AbstractRetrievePoints> LoadTagClassesRecorded(
            String inputFilePath,
            String outputDirectory,
            String timeResolution,
            int numYears,
            int pageSize,
            PIServer piServer,
            PISystem piSystem,
            int numParallelTasks,
            Logger logger)
        {

            var inputLines = LoadInputFile(inputFilePath);

            List<AbstractRetrievePoints> tagClasses = new List<AbstractRetrievePoints>();

            Parallel.ForEach(
                inputLines,
                new ParallelOptions { MaxDegreeOfParallelism = numParallelTasks },
                (String[] line) =>
                {
                    string tagName = line[0];
                    string startTimeString = line[1];
                    string endTimeString = line[2];

                    PIPoint tag;
                    try
                    {
                        tag = PIPoint.FindPIPoint(piServer, tagName);
                        AFTime startTime = new AFTime(startTimeString);
                        AFTime endTime = new AFTime(endTimeString);
                        AFTimeRange timeRange = new AFTimeRange(startTime, endTime);

                        string startTimeStamp = startTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
                        string endTimeStamp = endTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
                        lock (logger) { logger.Log($"{startTimeStamp} : {endTimeStamp}, {tagName}"); }
                        lock (tagClasses)
                        {
                            tagClasses.Add(new PIRecordedPointRetrievalClass(
                                tag,
                                timeRange,
                                outputDirectory,
                                PIRandomFunctionsUtil.ParseTimeResolutionString(timeResolution),
                                numYears,
                                pageSize,
                                logger));
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log("Exception: could not FindPiPoint: " + e.ToString());
                    }
                });

            return tagClasses;
        }


        public static List<AbstractRetrievePoints> LoadAttributeClassesRecorded(
            String inputFilePath,
            String outputDirectory,
            String timeResolution,
            int numYears,
            int pageSize,
            PIServer piServer,
            PISystem piSystem,
            AFDatabase database,
            int numParallelTasks,
            Logger logger)
        {
            var inputLines = new List<String[]>();
            using (var inputStreamReader = new StreamReader(inputFilePath))
            {
                while (!inputStreamReader.EndOfStream)
                {
                    inputLines.Add(inputStreamReader.ReadLine().Split(','));
                }
            }

            List<AbstractRetrievePoints> tagClasses = new List<AbstractRetrievePoints>();

            Parallel.ForEach(
                inputLines,
                new ParallelOptions { MaxDegreeOfParallelism = numParallelTasks },
                (String[] line) =>
                {
                    string tagName = line[0];
                    string startTimeString = line[1];
                    string endTimeString = line[2];

                    PIPoint tag;
                    try
                    {
                        AFAttribute attribute = AFUtil.FindAttribute(database, tagName);
                        tag = attribute.PIPoint;

                        AFTime startTime = new AFTime(startTimeString);
                        AFTime endTime = new AFTime(endTimeString);
                        AFTimeRange timeRange = new AFTimeRange(startTime, endTime);

                        string startTimeStamp = startTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
                        string endTimeStamp = endTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
                        lock (logger) { logger.Log($"{startTimeStamp} : {endTimeStamp}, {tagName}"); }
                        lock (tagClasses)
                        {
                            tagClasses.Add(new AFRecordedAttributeRetrievalClass(
                                attribute,
                                timeRange,
                                outputDirectory,
                                PIRandomFunctionsUtil.ParseTimeResolutionString(timeResolution),
                                numYears,
                                pageSize,
                                logger));
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log("Exception: could not FindPiPoint: " + e.ToString());
                    }
                });

            return tagClasses;
        }


        public static List<AbstractRetrievePoints> LoadTagClassesInterpolated(
            String inputFilePath,
            String outputDirectory,
            String timeResolution,
            int numYears,
            int pageSize,
            PIServer piServer,
            PISystem piSystem,
            int numParallelTasks,
            Logger logger)
        {
            var inputLines = new List<String[]>();
            using (var inputStreamReader = new StreamReader(inputFilePath))
            {
                while (!inputStreamReader.EndOfStream)
                {
                    inputLines.Add(inputStreamReader.ReadLine().Split(','));
                }
            }

            List<AbstractRetrievePoints> tagClasses = new List<AbstractRetrievePoints>();

            Parallel.ForEach(
                inputLines,
                new ParallelOptions { MaxDegreeOfParallelism = numParallelTasks },
                (String[] line) =>
                {
                    string tagName = line[0];
                    string startTimeString = line[1];
                    string endTimeString = line[2];
                    string intervalString = line[3];

                    PIPoint tag;
                    try
                    {
                        tag = PIPoint.FindPIPoint(piServer, tagName);

                        AFTime startTime = new AFTime(startTimeString);
                        AFTime nextTime = AFQueryUtil.GetNextRecordedValue(tag, startTime).Timestamp;
                        if (nextTime > startTime)
                        {
                            startTime = new AFTime(nextTime.UtcTime.Date);
                        }
                        AFTime endTime = new AFTime(endTimeString);
                        AFTimeRange timeRange = new AFTimeRange(startTime, endTime);
                        AFTimeSpan timeSpan = new AFTimeSpan(TimeSpan.FromSeconds(Int32.Parse(intervalString)));

                        string startTimeStamp = startTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
                        string endTimeStamp = endTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
                        lock (logger) { logger.Log($"{startTimeStamp} : {timeSpan} : {endTimeStamp}, {tagName}"); }

                        lock (tagClasses)
                        {
                            tagClasses.Add(new PIInterpolatedPointRetrievalClass(
                                tag,
                                timeRange,
                                timeSpan,
                                outputDirectory,
                                PIRandomFunctionsUtil.ParseTimeResolutionString(timeResolution),
                                numYears,
                                pageSize,
                                logger));
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log("Exception: could not FindPiPoint: " + e.ToString());
                    }
                });

            return tagClasses;
        }


        public static List<AbstractRetrievePoints> LoadAttributeClassesInterpolated(
            String inputFilePath,
            String outputDirectory,
            String timeResolution,
            int numYears,
            int pageSize,
            PIServer piServer,
            PISystem piSystem,
            AFDatabase database,
            int numParallelTasks,
            Logger logger)
        {
            var inputLines = new List<String[]>();
            using (var inputStreamReader = new StreamReader(inputFilePath))
            {
                while (!inputStreamReader.EndOfStream)
                {
                    inputLines.Add(inputStreamReader.ReadLine().Split(','));
                }
            }

            List<AbstractRetrievePoints> tagClasses = new List<AbstractRetrievePoints>();

            Parallel.ForEach(
                inputLines,
                new ParallelOptions { MaxDegreeOfParallelism = numParallelTasks },
                (String[] line) =>
                {
                    string tagName = line[0];
                    string startTimeString = line[1];
                    string endTimeString = line[2];
                    string intervalString = line[3];

                    PIPoint tag;
                    try
                    {
                        AFAttribute attribute = AFUtil.FindAttribute(database, tagName);
                        tag = attribute.PIPoint;

                        AFTime startTime = new AFTime(startTimeString);
                        AFTime nextTime = AFQueryUtil.GetNextRecordedValue(tag, startTime).Timestamp;
                        if (nextTime > startTime)
                        {
                            startTime = new AFTime(nextTime.UtcTime.Date);
                        }
                        AFTime endTime = new AFTime(endTimeString);
                        AFTimeRange timeRange = new AFTimeRange(startTime, endTime);
                        AFTimeSpan timeSpan = new AFTimeSpan(TimeSpan.FromSeconds(Int32.Parse(intervalString)));

                        string startTimeStamp = startTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
                        string endTimeStamp = endTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
                        lock (logger) { logger.Log($"{startTimeStamp} : {timeSpan} : {endTimeStamp}, {tagName}"); }

                        lock (tagClasses)
                        {
                            tagClasses.Add(new AFInterpolatedAttributeRetrievalClass(
                                attribute,
                                timeRange,
                                timeSpan,
                                outputDirectory,
                                PIRandomFunctionsUtil.ParseTimeResolutionString(timeResolution),
                                numYears,
                                pageSize,
                                logger));
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log("Exception: could not FindPiPoint: " + e.ToString());
                    }
                });
            return tagClasses;
        }


        public static void DoStuff(
            List<AbstractRetrievePoints> tagClasses,
            int numParallelTasks,
            Logger logger)
        {

            int numTags = tagClasses.Count;
            List<int> tagIndexes = new List<int>(numTags);

            for (int i = 0; i < numTags; i++)
            {
                tagIndexes.Add(i);
            }

            int tagBufferSize = Math.Min(numParallelTasks, numTags);
            List<int> tagBuffer = new List<int>(tagBufferSize);
            for (int i = 0; i < tagBufferSize; i++)
            {
                tagBuffer.Add(tagIndexes[0]);
                tagIndexes.RemoveAt(0);
            }

            foreach (int tagIndex in tagBuffer)
            {
                tagClasses[tagIndex].RunTaskToWriteDataToFile();
            }

            bool closedStatus;
            bool faultedStatus;
            int ii;
            while (tagBuffer.Count > 0)
            {
                Thread.Sleep(1);
                for (ii = tagBuffer.Count - 1; ii >= 0; ii--)
                {
                    faultedStatus = false;
                    if (tagClasses[tagBuffer[ii]].task.IsFaulted)
                    {
                        faultedStatus = true;
                        lock (logger)
                        {
                            foreach (var e in tagClasses[tagBuffer[ii]].task.Exception.InnerExceptions)
                            {
                                logger.Log($"{e.Message}");
                                logger.Log($"{e.StackTrace}");
                            }
                        }
                    }

                    lock (tagClasses[tagBuffer[ii]].closedLock)
                    {
                        closedStatus = tagClasses[tagBuffer[ii]].closed;
                    }

                    if (closedStatus || faultedStatus)
                    {
                        if (tagIndexes.Count > 0)
                        {
                            tagBuffer[ii] = tagIndexes[0];
                            tagIndexes.RemoveAt(0);
                            tagClasses[tagBuffer[ii]].RunTaskToWriteDataToFile();
                        }
                        else
                        {
                            tagBuffer.RemoveAt(ii);
                        }
                    }
                }
            }

        }


        public static void DoStuffParallelForeach(
            List<AbstractRetrievePoints> tagClasses,
            int numParallelTasks,
            Logger logger)
        {
            Parallel.ForEach(
                tagClasses,
                new ParallelOptions { MaxDegreeOfParallelism = numParallelTasks },
                (AbstractRetrievePoints retrievePoints) =>
                {
                    try
                    {
                        while (!retrievePoints.closed)
                        {
                            retrievePoints.GetNextResult();
                            retrievePoints.WriteNextResult();
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log($"{e.Message}");
                        logger.Log($"{e.StackTrace}");
                    }
                });
        }
    }
}
