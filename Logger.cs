using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace qqLogs
{
    class Logger
    {
        public static long Log_Size_Limit = 102400;
        public static uint numberOfOldLogsToKeep = 1;
        private static string logLineFormat = "%DateTime% - [%szLogLevel%] - %Message%";
        private static string logRoot = "logs/";
        private static string oldLogExtention = ".bak";
        private static string logName = "Log";
        private static iLogLevel LOG_LEVEL = iLogLevel.DEBUG;
        private static readonly object _custom = new object();

        public static List<string> szLogLevel = new List<string> { "Debug", "1", "Info", "3", "Warning", "5", "Error", "7", "Exception", "9", "FatalException" };

        #region Class Initializers
        /// <summary>
        /// Initialize the logging class with all defaults.
        /// </summary>
        /// <param name="filename">Filename of the log.</param>
        public static Logger(string filename)
        {
            logName = filename;
        }

        /// <summary>
        /// Initialize the logging class and set the Log Level property.
        /// </summary>
        /// <param name="filename">Filename for the log.</param>
        /// <param name="_logLevel">Log Level, value between 0 and 10.</param>
        public static Logger(string filename, iLogLevel _logLevel)
        {
            logName = filename;
            LOG_LEVEL = _logLevel;
        }

        /// <summary>
        /// Initialize the logging class and set LogLevel, Log Size Limit, and Number of Old Logs to Keep.
        /// </summary>
        /// <param name="filename">Filename of the log.</param>
        /// <param name="_logLevel">iLogLevel value.</param>
        /// <param name="_logSizeLimit">Log Size Limit in Bytes.</param>
        /// <param name="_numberOfOldLogsToKeep">How many old logs would you like to keep around.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static Logger(string filename, iLogLevel _logLevel, long _logSizeLimit, uint _numberOfOldLogsToKeep)
        {
            logName = filename;
            LOG_LEVEL = _logLevel;
            if (_logSizeLimit >= 0)
            {
                Log_Size_Limit = _logSizeLimit;
            }
            else
            {
                throw new ArgumentOutOfRangeException("_logSizeLimit", "Log Size Limit must be a non-negative value.");
            }
            numberOfOldLogsToKeep = _numberOfOldLogsToKeep;
        }
        #endregion
        #region GetSets for Private Properties
        public static string Log_Line_Format
        {
            get
            {
                return logLineFormat;
            }
            set
            {
                if (!value.Contains("%Message%"))
                {
                    logLineFormat = string.Format("{0} {1}", value, "%Message%");
                }
                else
                {
                    logLineFormat = value;
                }
            }
        }
        public static string Log_Root
        {
            get
            {
                return logRoot;
            }
            set
            {
                if (!value.EndsWith("/"))
                {
                    logRoot = string.Format("{0}/", value);
                }
                else
                {
                    logRoot = value;
                }
            }
        }
        public static string Old_Log_Extention
        {
            get
            {
                return oldLogExtention;
            }
            set
            {
                if (!value.StartsWith("."))
                {
                    oldLogExtention = string.Format(".{0}", value);
                }
                else
                {
                    oldLogExtention = value;
                }
            }
        }
        public static int Log_Level
        {
            get
            {
                return (int)LOG_LEVEL;
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
        public static void Log(iLogLevel logLevel, string data, string pre_fix = null, bool overwrite = false)
        {
            lock (_custom)
            {
                FileInfo fi = new FileInfo(logRoot + logName);
                StreamWriter sw;
                try
                {
                    if (!Directory.Exists(logRoot))
                        Directory.CreateDirectory(logRoot);
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
                                fi.CopyTo(string.Format("{0}{1}_{2}{3}", new object[] { logRoot, logName, DateTime.Now.ToFileTime().ToString(), oldLogExtention }), true);
                                fi.Delete();
                                CheckNumberofOldLogs();
                            }
                        }
                        sw = fi.AppendText();
                    }
                    if (!fi.Exists)
                    {
                        sw.WriteLine(FormatLogLine(0, "File Created", null));
                    }
                    if (logLevel >= LOG_LEVEL)
                    {
                        sw.WriteLine(FormatLogLine(logLevel, data, pre_fix));
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
        public static void Log(int logLevel, string data, string pre_fix = null, bool overwrite = false)
        {
            lock (_custom)
            {
                FileInfo fi = new FileInfo(logRoot + logName);
                StreamWriter sw;
                try
                {
                    if (!Directory.Exists(logRoot))
                        Directory.CreateDirectory(logRoot);
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
                                fi.CopyTo(string.Format("{0}{1}_{2}{3}", new object[] { logRoot, logName, DateTime.Now.ToFileTime().ToString(), oldLogExtention }), true);
                                fi.Delete();
                                CheckNumberofOldLogs();
                            }
                        }
                        sw = fi.AppendText();
                    }
                    if (!fi.Exists)
                    {
                        sw.WriteLine(FormatLogLine(0, "File Created", null));
                    }
                    if (logLevel >= (int)LOG_LEVEL)
                    {
                        sw.WriteLine(FormatLogLine(logLevel, data, pre_fix));
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

        private static string FormatLogLine(iLogLevel _logLevel, string _Message, string preFix)
        {
            string logLine = logLineFormat;
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

        private static string FormatLogLine(int _logLevel, string _Message, string preFix)
        {
            string logLine = logLineFormat;
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
        private static void CheckNumberofOldLogs()
        {
            DirectoryInfo logroot = new DirectoryInfo(logRoot);
            var logfiles = from files in logroot.EnumerateFiles(string.Format("{0}*{1}", logName, oldLogExtention))
                           orderby files.CreationTime
                           select files;
            if (logfiles != null && logfiles.Any())
            {
                int numberOfOldLogs = logfiles.Count();
                if (numberOfOldLogs > numberOfOldLogsToKeep)
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
