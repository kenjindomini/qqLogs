/* Logging.cs
 * QuasarQode logging Library v0.5
 * 
 * 18-12-2013 - Keith Olenchak
 * -Started converting this in to a Library.
 * -Made mehtods non-static
 * -Added 3 class initializers.
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
        private string logLineFormat = null; //not implemented yet.

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
        #endregion
        
        public int Custom(string filename, string data, string pre_fix = null, bool overwrite = false)
        {
            int RetVal = 0;
            lock (_custom)
            {
                FileInfo fi = new FileInfo("logs/" + filename);
                StreamWriter sw;
                try
                {
                    if (!Directory.Exists("logs"))
                        Directory.CreateDirectory("logs");
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
                                fi.CopyTo("logs/" + filename + ".bak", true);
                                fi.Delete();
                            }
                        }
                        sw = fi.AppendText();
                    }
                    if (!fi.Exists)
                    {
                        sw.WriteLine(DateTime.Now.ToString() + " - [0] - File Created");
                    }
                    if (pre_fix != null)
                        sw.WriteLine(pre_fix + DateTime.Now.ToString() + " - " + data);
                    else
                        sw.WriteLine(DateTime.Now.ToString() + " - " + data);
                    sw.Close();
                    sw.Dispose();
                }
                catch (UnauthorizedAccessException e)
                {
                    MessageBox.Show("UnauthorizedAccessException caught in QuasarQode.logs: " + e.Message, "Unauthorized Access Exception", MessageBoxButtons.OK);
                    RetVal = 1001;
                }
                catch (IOException e)
                {
                    MessageBox.Show("IOException caught in QuasarQode.logs: " + e.Message, "Generic IO Exception", MessageBoxButtons.OK);
                    RetVal = 1002;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Generic Exception caught in QuasarQode.logs: " + e.Message, "Generic Exception", MessageBoxButtons.OK);
                    RetVal = 1003;
                }
            }
            return RetVal;
        }
    }
}
