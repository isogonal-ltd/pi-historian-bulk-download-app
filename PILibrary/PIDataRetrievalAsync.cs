using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using OSIsoft.AF.UnitsOfMeasure;
using System;
using System.IO;
using System.Threading.Tasks;

// https://techsupport.osisoft.com/Documentation/PI-AF-SDK/html/M_OSIsoft_AF_Data_AFListData_InterpolatedValues.htm

namespace PILibrary
{
    public abstract class AbstractRetrievePoints
    {
        public abstract void GetNextResult();
        public abstract void WriteNextResult();

        public readonly object closedLock = new object();
        public bool closed = false;
        public Task task;
        // only required for the custom buffer function,
        // not the parallel foreach
        public void RunTaskToWriteDataToFile()
        {
            task = new Task(() =>
            {
                while (!this.closed)
                {
                    this.GetNextResult();
                    this.WriteNextResult();
                }
            });
            task.Start();
        }
    }

    public abstract class AbstractRetrieveRecordedPoints : AbstractRetrievePoints
    {
        protected int exceptionCount = 0;

        protected PIPoint tag;
        protected AFTimeRange timeRange;
        protected string outputDirectory;
        protected PIRandomFunctionsUtil.TimeResolution timeResolution;
        protected int numYears;
        protected int pageSize;
        protected Logger logger;

        protected AFTime nextStartTime;
        protected AFTimeRange pageTimeRange;

        protected int skipCount = 0;
        protected bool fetchNextPage = true;

        protected int k = 0;
        protected StreamWriter outputStreamWriter = null;
        protected string lastDatePath = null;

        protected Task<AFValues> valuesTask;
        protected AFValues values;

        public void Close()
        {
            outputStreamWriter.Close();
        }
    }

    public class PIRecordedPointRetrievalClass : AbstractRetrieveRecordedPoints
    {

        public PIRecordedPointRetrievalClass(
            PIPoint tag,
            AFTimeRange timeRange,
            string outputDirectory,
            PIRandomFunctionsUtil.TimeResolution timeResolution,
            int numYears,
            int pageSize = 200000,
            Logger logger = null)
        {
            this.tag = tag;
            this.timeRange = timeRange;
            this.nextStartTime = timeRange.StartTime;
            this.outputDirectory = outputDirectory;
            this.timeResolution = timeResolution;
            this.numYears = numYears;
            this.pageSize = pageSize;
            if (logger == null)
            {
                logger = new Logger();
            }
            this.logger = logger;
        }

        override public void GetNextResult()
        {
            pageTimeRange = new AFTimeRange(nextStartTime, timeRange.EndTime);
            valuesTask = tag.RecordedValuesAsync(pageTimeRange, AFBoundaryType.Inside, filterExpression: null, includeFilteredValues: false, maxCount: this.pageSize);
        }

        override public void WriteNextResult()
        {
            try
            {
                values = valuesTask.Result;
            }
            catch (PITimeoutException)
            {
                fetchNextPage = true;
                exceptionCount++;
                if (exceptionCount > 10)
                    throw new Exception("EXCEPTION: Too many retries: " + this.tag.Name);
                return;
            }

            string nextStartTimeStamp = nextStartTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
            string nextEndTimeStamp = timeRange.EndTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
            lock (logger)
            {
                logger.Log($"{k}, {nextStartTimeStamp} : {nextEndTimeStamp}, {values.Count}, {tag.Name}");
            }
            k++;

            for (int i = skipCount; i < values.Count; i++)
            {
                string datePath = PIRandomFunctionsUtil.DateTimeToDatePath(values[i].Timestamp, timeResolution, numYears);
                if (outputStreamWriter == null || lastDatePath != datePath)
                {
                    string outputPath = Path.Combine(outputDirectory, datePath, tag.Name);
                    Directory.CreateDirectory(new FileInfo(outputPath).Directory.FullName);
                    if (outputStreamWriter != null)
                    {
                        outputStreamWriter.Close();
                    }
                    outputStreamWriter = new StreamWriter(outputPath);
                    lastDatePath = datePath;
                }
                outputStreamWriter.WriteLine($"{values[i].Timestamp.UtcTime.ToString("o")},{values[i].Value}");
            }

            fetchNextPage = values.Count >= pageSize;
            if (fetchNextPage)
            {
                int lastIndex = values.Count - 1;
                skipCount = 1;
                nextStartTime = values[lastIndex].Timestamp;
                for (int i = lastIndex - 1; i >= 0; --i)
                {
                    if (values[i].Timestamp == nextStartTime)
                    {
                        ++skipCount;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (!fetchNextPage)
            {
                this.Close();
                lock (closedLock) { closed = true; }
            }
        }
    }

    public class AFRecordedAttributeRetrievalClass : AbstractRetrieveRecordedPoints
    {
        readonly AFAttribute attribute;
        readonly UOM desiredUOM;

        public AFRecordedAttributeRetrievalClass(
            AFAttribute attribute,
            AFTimeRange timeRange,
            string outputDirectory,
            PIRandomFunctionsUtil.TimeResolution timeResolution,
            int numYears,
            int pageSize = 200000,
            Logger logger = null)
        {
            this.attribute = attribute;
            this.tag = attribute.PIPoint;
            if (tag == null)
            {
                throw new ArgumentException($"attribute PIPoint must be not null");
            }
            this.desiredUOM = attribute.DefaultUOM;

            this.timeRange = timeRange;
            this.outputDirectory = outputDirectory;
            this.pageSize = pageSize;
            this.nextStartTime = timeRange.StartTime;
            this.timeResolution = timeResolution;
            this.numYears = numYears;
            this.pageSize = pageSize;
            if (logger == null)
            {
                logger = new Logger();
            }
            this.logger = logger;
        }

        override public void GetNextResult()
        {
            pageTimeRange = new AFTimeRange(nextStartTime, timeRange.EndTime);
            valuesTask = tag.RecordedValuesAsync(pageTimeRange, AFBoundaryType.Inside, filterExpression: null, includeFilteredValues: false, maxCount: this.pageSize);
        }

        override public void WriteNextResult()
        {
            try
            {
                values = valuesTask.Result;
            }
            catch (PITimeoutException)
            {
                fetchNextPage = true;
                exceptionCount++;
                if (exceptionCount > 10)
                    throw new Exception("EXCEPTION: Too many retries: " + this.tag.Name);
                return;
            }

            string nextStartTimeStamp = nextStartTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
            string nextEndTimeStamp = timeRange.EndTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
            lock (logger)
            {
                logger.Log($"{k}, {nextStartTimeStamp} : {nextEndTimeStamp}, {values.Count}, {tag.Name}");
            }
            k++;
            for (int i = skipCount; i < values.Count; i++)
            {
                string datePath = PIRandomFunctionsUtil.DateTimeToDatePath(values[i].Timestamp, timeResolution, numYears);
                if (outputStreamWriter == null || lastDatePath != datePath)
                {
                    string outputPath = Path.Combine(outputDirectory, datePath, tag.Name);
                    Directory.CreateDirectory(new FileInfo(outputPath).Directory.FullName);
                    if (outputStreamWriter != null)
                    {
                        outputStreamWriter.Close();
                    }
                    outputStreamWriter = new StreamWriter(outputPath);
                    lastDatePath = datePath;
                }
                AFValue value = values[i];
                value.Attribute = attribute;
                if (value.IsGood && (desiredUOM != null) && (desiredUOM != value.UOM))
                {
                    outputStreamWriter.WriteLine($"{value.Timestamp.UtcTime.ToString("o")},{value.Convert(desiredUOM).Value}");
                }
                else
                {
                    outputStreamWriter.WriteLine($"{value.Timestamp.UtcTime.ToString("o")},{value.Value}");
                }
            }

            fetchNextPage = values.Count >= pageSize;
            if (fetchNextPage)
            {
                int lastIndex = values.Count - 1;
                skipCount = 1;
                nextStartTime = values[lastIndex].Timestamp;
                for (int i = lastIndex - 1; i >= 0; --i)
                {
                    if (values[i].Timestamp == nextStartTime)
                    {
                        ++skipCount;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (!fetchNextPage)
            {
                this.Close();
                lock (closedLock) { closed = true; }
            }
        }
    }

    public abstract class AbstractRetrieveInterpolatedPoints: AbstractRetrievePoints
    {
        protected int exceptionCount = 0;

        protected PIPoint tag;
        protected AFTimeRange timeRange;
        protected AFTimeSpan timeSpan;
        protected string outputDirectory;
        protected PIRandomFunctionsUtil.TimeResolution timeResolution;
        protected int numYears;
        protected int pageSize;
        protected Logger logger;

        protected AFTime nextStartTime;
        protected AFTime nextEndTime;
        protected AFTimeRange pageTimeRange;

        protected int skipCount = 0;
        protected bool fetchNextPage = true;

        protected Task<AFValues> valuesTask;
        protected AFValues values;

        protected int k = 0;
        protected StreamWriter outputStreamWriter = null;
        protected string lastDatePath = null;

        public void Close()
        {
            outputStreamWriter.Close();
        }
    }



    public class PIInterpolatedPointRetrievalClass: AbstractRetrieveInterpolatedPoints
    {

        public PIInterpolatedPointRetrievalClass(
            PIPoint tag, 
            AFTimeRange timeRange, 
            AFTimeSpan timeSpan, 
            string outputDirectory,
            PIRandomFunctionsUtil.TimeResolution timeResolution, 
            int numYears, 
            int pageSize = 200000,
            Logger logger = null)
        {
            this.tag = tag;
            this.timeRange = timeRange;
            this.timeSpan = timeSpan;
            this.nextStartTime = timeRange.StartTime;
            this.outputDirectory = outputDirectory;
            this.timeResolution = timeResolution;
            this.numYears = numYears;
            this.pageSize = pageSize;
            if (logger == null)
            {
                logger = new Logger();
            }
            this.logger = logger;
        }


        override public void GetNextResult()
        {

            nextEndTime = timeSpan.Multiply(nextStartTime, pageSize);
            if (nextEndTime > timeRange.EndTime)
            {
                nextEndTime = timeRange.EndTime;
                fetchNextPage = false;
            }
            pageTimeRange = new AFTimeRange(nextStartTime, nextEndTime);
            valuesTask = tag.InterpolatedValuesAsync(pageTimeRange, timeSpan, filterExpression: null, includeFilteredValues: false);
        }

        override public void WriteNextResult()
        {
            try
            {
                values = valuesTask.Result;
            }
            catch (PITimeoutException)
            {
                fetchNextPage = true;
                exceptionCount++;
                if (exceptionCount > 10)
                    throw new Exception("EXCEPTION: Too many retries: " + this.tag.Name);
                return;
            }

            string nextStartTimeStamp = nextStartTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
            string nextEndTimeStamp = timeRange.EndTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
            lock (logger)
            { 
                logger.Log($"{k}, {nextStartTimeStamp} : {timeSpan} : {nextEndTimeStamp}, {values.Count}, {tag.Name}");
            }
            k++;

            for (int i = skipCount; i < values.Count; i++)
            {
                string datePath = PIRandomFunctionsUtil.DateTimeToDatePath(values[i].Timestamp, timeResolution, numYears);
                if (outputStreamWriter == null || lastDatePath != datePath)
                {
                    string outputPath = Path.Combine(outputDirectory, datePath, tag.Name);
                    Directory.CreateDirectory(new FileInfo(outputPath).Directory.FullName);
                    if (outputStreamWriter != null)
                    {
                        outputStreamWriter.Close();
                    }
                    outputStreamWriter = new StreamWriter(outputPath);
                    lastDatePath = datePath;
                }
                outputStreamWriter.WriteLine($"{values[i].Timestamp.UtcTime.ToString("o")},{values[i].Value}");
            }
            
            if (fetchNextPage)
            {
                int lastIndex = values.Count - 1;
                skipCount = 1;
                nextStartTime = values[lastIndex].Timestamp;
                for (int i = lastIndex - 1; i >= 0; --i)
                {
                    if (values[i].Timestamp == nextStartTime)
                    {
                        ++skipCount;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (!fetchNextPage)
            {
                this.Close();
                lock (closedLock) { closed = true; }
            }
        }

    }

    public class AFInterpolatedAttributeRetrievalClass: AbstractRetrieveInterpolatedPoints
    {

        readonly AFAttribute attribute;
        readonly UOM desiredUOM;

        public AFInterpolatedAttributeRetrievalClass(
            AFAttribute attribute, 
            AFTimeRange timeRange, 
            AFTimeSpan timeSpan, 
            string outputDirectory,
            PIRandomFunctionsUtil.TimeResolution timeResolution, 
            int numYears, 
            int pageSize = 200000,
            Logger logger = null)
        {
            this.attribute = attribute;
            this.tag = attribute.PIPoint;
            if (tag == null)
            {
                throw new ArgumentException($"attribute PIPoint must be not null");
            }
            this.desiredUOM = attribute.DefaultUOM;

            this.timeRange = timeRange;
            this.timeSpan = timeSpan;
            this.outputDirectory = outputDirectory;
            this.timeResolution = timeResolution;
            this.numYears = numYears;
            this.pageSize = pageSize;
            this.nextStartTime = timeRange.StartTime;
            if (logger == null)
            {
                logger = new Logger();
            }
            this.logger = logger;
        }


        override public void GetNextResult()
        {
            
            nextEndTime = timeSpan.Multiply(nextStartTime, pageSize);
            if (nextEndTime > timeRange.EndTime)
            {
                nextEndTime = timeRange.EndTime;
                fetchNextPage = false;
            }
            pageTimeRange = new AFTimeRange(nextStartTime, nextEndTime);
            valuesTask = tag.InterpolatedValuesAsync(pageTimeRange, timeSpan, filterExpression: null, includeFilteredValues: false);
        }

        override public void WriteNextResult()
        {
            try
            {
                values = valuesTask.Result;
            }
            catch (PITimeoutException)
            {
                fetchNextPage = true;
                exceptionCount++;
                if (exceptionCount > 10)
                    throw new Exception("EXCEPTION: Too many retries: " + this.tag.Name);
                return;
            }

            string nextStartTimeStamp = nextStartTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
            string nextEndTimeStamp = timeRange.EndTime.UtcTime.ToString("yyyy/MM/dd HH:mm:ss");
            lock(logger)
            {
                logger.Log($"{k}, {nextStartTimeStamp} : {timeSpan} : {nextEndTimeStamp}, {values.Count}, {tag.Name}");
            }
            k++;
            
            for (int i = skipCount; i < values.Count; i++)
            {
                string datePath = PIRandomFunctionsUtil.DateTimeToDatePath(values[i].Timestamp, timeResolution, numYears);
                if (outputStreamWriter == null || lastDatePath != datePath)
                {
                    string outputPath = Path.Combine(outputDirectory, datePath, tag.Name);
                    Directory.CreateDirectory(new FileInfo(outputPath).Directory.FullName);
                    if (outputStreamWriter != null)
                    {
                        outputStreamWriter.Close();
                    }
                    outputStreamWriter = new StreamWriter(outputPath);
                    lastDatePath = datePath;
                }
                AFValue value = values[i];
                value.Attribute = attribute;
                if (value.IsGood && (desiredUOM != null) && (desiredUOM != value.UOM))
                {
                    outputStreamWriter.WriteLine($"{value.Timestamp.UtcTime.ToString("o")},{value.Convert(desiredUOM).Value}");
                }
                else
                {
                    outputStreamWriter.WriteLine($"{value.Timestamp.UtcTime.ToString("o")},{value.Value}");
                }
            }

            if (fetchNextPage)
            {
                int lastIndex = values.Count - 1;
                skipCount = 1;
                nextStartTime = values[lastIndex].Timestamp;
                for (int i = lastIndex - 1; i >= 0; --i)
                {
                    if (values[i].Timestamp == nextStartTime)
                    {
                        ++skipCount;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (!fetchNextPage)
            {
                this.Close();
                lock (closedLock) { closed = true; }
            }
        }
    }
}
