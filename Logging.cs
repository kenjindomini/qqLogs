/* Logging.cs
 * QuasarQode logging Library
 * 
 * 13-01-2014 - Keith Olenchak
 * -Made a change to the DateTime value that is being appended to the log name when it is copied to a bak file, should fix the "NotSupported" exception.
 * 
 * 18-12-2013 - Keith Olenchak
 * -Started converting this in to a Library.
 * -Made mehtods non-static
 * -Added 3 class initializers.
 * -Log line format can be set and will be adhered to even for the "file created" line.
 * -checks if the number of old logs exceeds the value of old logs to keep.
 * -public variables have been set for all private variables that need to be accessed.
 * -LOG_LEVEL made private again. Created a public version with get/set and set will fallback to more verbose if an int is passed that cannot be converted to the iLogLevel enum type.
 * -LogLevels are also now enforced.
 * -Added an overload for Log() that accepts the loglevel in the form of an int.
 * -Added an overload for FormatLogLine() that accepts the loglevel in the form of an int.
 * 06-12-2013 - Keith Olenchak
 * -Added iLogLevel enum.
 * -Added szLogLevel list to convert loglevel to a string.
 * 
 * 13-12-2012 - Keith Olenchak
 * Added optional argument 'pre_fix' to custom(). This will insert any text prior to the datetime stamp. Primary purpose is for adding 2 carriage returns between previous executions
 * and the current execution's log entries.
 * 
 * 29-07-2012 - Keith Olenchak
 * Logs are now thread safe
 * Logs also now have a size limit of 100KB, they are backed up to a .bak file that gets overwritten every time.
 * 
 * 25-07-2012 - Keith Olenchak
 * I broke the logging class out in to its own CS file and thus its own name space "Quasar.Logs".
 */

using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace qqLogs
{
    public enum iLogLevel : int { DEBUG = 0, INFO = 2, WARNING = 4, ERROR = 6, EXCEPTION = 8, FATALEXCEPTION = 10 };
    public class Logging
    {
        public long Log_Size_Limit = 102400;
        public uint numberOfOldLogsToKeep = 1;
        private string logLineFormat = "%DateTime% - [%szLogLevel%] - %Message%";
        private string logRoot = "logs/";
        private string oldLogExtention = ".bak";
        private string logName = "Log";
        private iLogLevel LOG_LEVEL = iLogLevel.DEBUG;
        private readonly object _custom = new object();

        public static List<string> szLogLevel = new List<string> { "Debug", "1", "Info", "3", "Warning", "5", "Error", "7", "Exception", "9", "FatalException" };

        #region Class Initializers
        /// <summary>
        /// Initialize the logging class with all defaults.
        /// </summary>
        /// <param name="filename">Filename of the log.</param>
        public Logging(string filename)
        {
            this.logName = filename;
        }

        /// <summary>
        /// Initialize the logging class and set the Log Level property.
        /// </summary>
        /// <param name="filename">Filename for the log.</param>
        /// <param name="_logLevel">Log Level, value between 0 and 10.</param>
        public Logging(string filename, iLogLevel _logLevel)
        {
            this.logName = filename;
            this.LOG_LEVEL = _logLevel;
        }

        /// <summary>
        /// Initialize the logging class and set LogLevel, Log Size Limit, and Number of Old Logs to Keep.
        /// </summary>
        /// <param name="filename">Filename of the log.</param>
        /// <param name="_logLevel">iLogLevel value.</param>
        /// <param name="_logSizeLimit">Log Size Limit in Bytes.</param>
        /// <param name="_numberOfOldLogsToKeep">How many old logs would you like to keep around.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public Logging(string filename, iLogLevel _logLevel, long _logSizeLimit, uint _numberOfOldLogsToKeep)
        {
            this.logName = filename;
            this.LOG_LEVEL = _logLevel;
            if (_logSizeLimit >= 0)
            {
                this.Log_Size_Limit = _logSizeLimit;
            }
            else
            {
                throw new ArgumentOutOfRangeException("_logSizeLimit", "Log Size Limit must be a non-negative value.");
            }
            this.numberOfOldLogsToKeep = _numberOfOldLogsToKeep;
        }
        #endregion
        #region GetSets for Private Properties
        public string Log_Line_Format
        {
            get
            {
                return this.logLineFormat;
            }
            set
            {
                if (!value.Contains("%Message%"))
                {
                    this.logLineFormat = string.Format("{0} {1}", value, "%Message%");
                }
                else
                {
                    this.logLineFormat = value;
                }
            }
        }
        public string Log_Root
        {
            get
            {
                return this.logRoot;
            }
            set
            {
                if (!value.EndsWith("/"))
                {
                    this.logRoot = string.Format("{0}/", value);
                }
                else
                {
                    this.logRoot = value;
                }
            }
        }
        public string Old_Log_Extention
        {
            get
            {
                return this.oldLogExtention;
            }
            set
            {
                if (!value.StartsWith("."))
                {
                    this.oldLogExtention = string.Format(".{0}", value);
                }
                else
                {
                    this.oldLogExtention = value;
                }
            }
        }
        public int Log_Level
        {
            get
            {
                return (int)this.LOG_LEVEL;
            }
            set
            {
                if (Enum.IsDefined(typeof(iLogLevel), value))
                {
                    LOG_LEVEL = (iLogLevel)value;
                }
                else
                {
                    value--;
                    if (Enum.IsDefined(typeof(iLogLevel), value))
                    {
                        LOG_LEVEL = (iLogLevel)value;
                    }
                }
            }
        }
        #endregion
        /// <summary>
        /// Log a message to specifieced log file at the specified log level.
        /// </summary>
        /// <param name="logLevel">Log level to log this message at.</param>
        /// <param name="data">Message to be logged.</param>
        /// <param name="pre_fix">Optional string to prefix the log line with.</param>
        /// <param name="overwrite">Optional. If true the log file will be overwritten with the new data.</param>
        public void Log(iLogLevel logLevel, string data, string pre_fix = null, bool overwrite = false)
        {
            lock (_custom)
            {
                FileInfo fi = new FileInfo(this.logRoot + this.logName);
                StreamWriter sw;
                try
                {
                    if (!Directory.Exists(this.logRoot))
                        Directory.CreateDirectory(this.logRoot);
                    if (overwrite)
                    {
                        fi.Delete();
                        sw = fi.CreateText();
                    }
                    else
                    {
                        if (fi.Exists && !overwrite)
                        {
                            if (fi.Length > Log_Size_Limit)
                            {
                                fi.CopyTo(string.Format("{0}{1}_{2}{3}", new object[] { this.logRoot, this.logName, DateTime.Now.ToFileTime().ToString(), this.oldLogExtention }), true);
                                fi.Delete();
                                this.CheckNumberofOldLogs();
                            }
                        }
                        sw = fi.AppendText();
                    }
                    if (!fi.Exists)
                    {
                        sw.WriteLine(this.FormatLogLine(0, "File Created", null));
                    }
                    if (logLevel >= this.LOG_LEVEL)
                    {
                        sw.WriteLine(this.FormatLogLine(logLevel, data, pre_fix));
                    }
                    sw.Close();
                    sw.Dispose();
                }
                catch (UnauthorizedAccessException e)
                {
                    MessageBox.Show("UnauthorizedAccessException caught in qqlogs: " + e.Message, "Unauthorized Access Exception", MessageBoxButtons.OK);
                }
                catch (IOException e)
                {
                    MessageBox.Show("IOException caught in qqlogs: " + e.Message, "Generic IO Exception", MessageBoxButtons.OK);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Generic Exception caught in qqlogs: " + e.Message, "Generic Exception", MessageBoxButtons.OK);
                }
            }
        }

        /// <summary>
        /// Log a message to specifieced log file at the specified log level.
        /// </summary>
        /// <param name="logLevel">Log level to log this message at.</param>
        /// <param name="data">Message to be logged.</param>
        /// <param name="pre_fix">Optional string to prefix the log line with.</param>
        /// <param name="overwrite">Optional. If true the log file will be overwritten with the new data.</param>
        public void Log(int logLevel, string data, string pre_fix = null, bool overwrite = false)
        {
            lock (_custom)
            {
                FileInfo fi = new FileInfo(this.logRoot + this.logName);
                StreamWriter sw;
                try
                {
                    if (!Directory.Exists(this.logRoot))
                        Directory.CreateDirectory(this.logRoot);
                    if (overwrite)
                    {
                        fi.Delete();
                        sw = fi.CreateText();
                    }
                    else
                    {
                        if (fi.Exists && !overwrite)
                        {
                            if (fi.Length > Log_Size_Limit)
                            {
                                fi.CopyTo(string.Format("{0}{1}_{2}{3}", new object[] { this.logRoot, this.logName, DateTime.Now.ToFileTime().ToString(), this.oldLogExtention }), true);
                                fi.Delete();
                                this.CheckNumberofOldLogs();
                            }
                        }
                        sw = fi.AppendText();
                    }
                    if (!fi.Exists)
                    {
                        sw.WriteLine(this.FormatLogLine(0, "File Created", null));
                    }
                    if (logLevel >= (int)this.LOG_LEVEL)
                    {
                        sw.WriteLine(this.FormatLogLine(logLevel, data, pre_fix));
                    }
                    sw.Close();
                    sw.Dispose();
                }
                catch (UnauthorizedAccessException e)
                {
                    MessageBox.Show("UnauthorizedAccessException caught in qqlogs: " + e.Message, "Unauthorized Access Exception", MessageBoxButtons.OK);
                }
                catch (IOException e)
                {
                    MessageBox.Show("IOException caught in qqlogs: " + e.Message, "Generic IO Exception", MessageBoxButtons.OK);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Generic Exception caught in qqlogs: " + e.Message, "Generic Exception", MessageBoxButtons.OK);
                }
            }
        }

        private string FormatLogLine(iLogLevel _logLevel, string _Message, string preFix)
        {
            string logLine = this.logLineFormat;
            if (preFix != null && preFix != string.Empty)
            {
                logLine = string.Format("{0}{1}", preFix, logLine);
            }
            logLine = logLine.Replace("%DateTime%", DateTime.Now.ToString());
            logLine = logLine.Replace("%szLogLevel%", szLogLevel[(int)_logLevel]);
            logLine = logLine.Replace("%LogLevel%", _logLevel.ToString());
            logLine = logLine.Replace("%Message%", _Message);
            return logLine;
        }

        private string FormatLogLine(int _logLevel, string _Message, string preFix)
        {
            string logLine = this.logLineFormat;
            if (preFix != null && preFix != string.Empty)
            {
                logLine = string.Format("{0}{1}", preFix, logLine);
            }
            logLine = logLine.Replace("%DateTime%", DateTime.Now.ToString());
            logLine = logLine.Replace("%szLogLevel%", szLogLevel[_logLevel]);
            logLine = logLine.Replace("%LogLevel%", _logLevel.ToString());
            logLine = logLine.Replace("%Message%", _Message);
            return logLine;
        }
        private void CheckNumberofOldLogs()
        {
            DirectoryInfo logroot = new DirectoryInfo(string.Format("{0}{1}", ".", this.logRoot));
            var logfiles = from files in logroot.EnumerateFiles(string.Format("{0}*{1}", this.logName, this.oldLogExtention))
                           orderby files.CreationTime
                           select files;
            if (logfiles != null && logfiles.Any())
            {
                int numberOfOldLogs = logfiles.Count();
                if (numberOfOldLogs > this.numberOfOldLogsToKeep)
                {
                    try
                    {
                        FileInfo oldestlog = logfiles.ElementAt(0);
                        oldestlog.Delete();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Generic Exception caught in qqlogs: " + e.Message, "Generic Exception", MessageBoxButtons.OK);
                    }
                }
            }
        }
    }
}
