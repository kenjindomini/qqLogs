/* Logging.cs
 * QuasarQode logging Library
 * 
 * 18-12-2013 - Keith Olenchak
 * -Started converting this in to a Library.
 * -Made mehtods non-static
 * -Added 3 class initializers.
 * -Log line format can be set and will be adhered to even for the "file created" line.
 * 
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

namespace qqLogs
{
    public class Logging
    {
        private int LOG_LEVEL = 0;
        private long Log_Size_Limit = 102400;
        private uint numberOfOldLogsToKeep = 1; //not implemented yet.
        private readonly object _qqsync = new object();
        private readonly object _custom = new object();
        private string logLineFormat = "%DateTime% - [%szLogLevel%] - %Message%";
        private string logRoot = "logs/";
        private string oldLogExtentionBase = ".bak";

        public enum iLogLevel : int { DEBUG = 0, INFO = 2, WARNING = 4, ERROR = 6, EXCEPTION = 8, FATALEXCEPTION = 10 };
        public static List<string> szLogLevel = new List<string> { "Debug", "1", "Info", "3", "Warning", "5", "Error", "7", "Exception", "9", "FatalException" };

        #region Class Initializers
        /// <summary>
        /// Initialize the logging class with all defaults.
        /// </summary>
        public Logging()
        {
        }

        /// <summary>
        /// Initialize the logging class and set the Log Level property.
        /// </summary>
        /// <param name="_logLevel">Log Level, value between 0 and 10.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when _logLevel is not between 0 and 10.</exception>
        public Logging(int _logLevel)
        {
            if (_logLevel >= 0 && _logLevel < 11)
            {
                this.LOG_LEVEL = _logLevel;
            }
            else
            {
                throw new ArgumentOutOfRangeException("_logLevel", "Log Level must be a value between 0 and 10.");
            }
        }

        /// <summary>
        /// Initialize the logging class and set LogLevel, Log Size Limit, and Number of Old Logs to Keep.
        /// </summary>
        /// <param name="_logLevel">Log Level, value between 0 and 10.</param>
        /// <param name="_logSizeLimit">Log Size Limit in Bytes.</param>
        /// <param name="_numberOfOldLogsToKeep">How many old logs would you like to keep around.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public Logging(int _logLevel, long _logSizeLimit, uint _numberOfOldLogsToKeep)
        {
            if (_logLevel >= 0 && _logLevel < 11)
            {
                this.LOG_LEVEL = _logLevel;
            }
            else
            {
                throw new ArgumentOutOfRangeException("_logLevel", "Log Level must be a value between 0 and 10.");
            }
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
        public int Log_Level
        {
            get
            {
                return this.LOG_LEVEL;
            }
            set
            {
                if (value >= 0 && value < 11)
                {
                    this.LOG_LEVEL = value;
                }
            }
        }
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
        #endregion
        /// <summary>
        /// Log a message to specifieced log file at the specified log level.
        /// </summary>
        /// <param name="filename">Name of log file including extention.</param>
        /// <param name="logLevel">Log level to log this message at.</param>
        /// <param name="data">Message to be logged.</param>
        /// <param name="pre_fix">Optional string to prefix the log line with.</param>
        /// <param name="overwrite">Optional. If true the log file will be overwritten with the new data.</param>
        public void Log(string filename, iLogLevel logLevel, string data, string pre_fix = null, bool overwrite = false)
        {
            lock (_custom)
            {
                FileInfo fi = new FileInfo(this.logRoot + filename);
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
                                fi.CopyTo(this.logRoot + filename + this.oldLogExtentionBase, true);
                                fi.Delete();
                            }
                        }
                        sw = fi.AppendText();
                    }
                    if (!fi.Exists)
                    {
                        sw.WriteLine(this.FormatLogLine(0, "File Created", null));
                    }
                    sw.WriteLine(this.FormatLogLine(logLevel, data, pre_fix));
                    sw.Close();
                    sw.Dispose();
                }
                catch (UnauthorizedAccessException e)
                {
                    MessageBox.Show("UnauthorizedAccessException caught in QuasarQode.logs: " + e.Message, "Unauthorized Access Exception", MessageBoxButtons.OK);
                }
                catch (IOException e)
                {
                    MessageBox.Show("IOException caught in QuasarQode.logs: " + e.Message, "Generic IO Exception", MessageBoxButtons.OK);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Generic Exception caught in QuasarQode.logs: " + e.Message, "Generic Exception", MessageBoxButtons.OK);
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
            if (logLine.Contains("%DateTime%"))
            {
                logLine.Replace("%DateTime%", DateTime.Now.ToString());
            }
            if (logLine.Contains("%szLogLevel%"))
            {
                logLine.Replace("%szLogLevel%", szLogLevel[(int)_logLevel]);
            }
            if (logLine.Contains("%LogLevel%"))
            {
                logLine.Replace("%LogLevel%", _logLevel.ToString());
            }
            if (logLine.Contains("%Message%"))
            {
                logLine.Replace("%Message%", _Message);
            }
            return logLine;
        }
    }
}
