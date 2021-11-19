using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Globalization;

using Hylasoft.Opc;

namespace opc_stream
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("opc-stream - reads time-stamped tags from a csv-file into an OPC-server ");
            Console.WriteLine("     usage:  ");
            Console.WriteLine("         case 1: opc-stream : all configuration is read from opc-stream.exe.config ");
            Console.WriteLine("         case 2: opc-stream [csvfile]: streams specified file, all other configuration from opc-stream.exe.config ");
            Console.WriteLine("             example: \"stream FileName.csv\"");
            Console.WriteLine("         case 3: opc-stream [csvfile] [-s startTime] [-e endTime] : streams specified between two given timestamps ");
            Console.WriteLine("             example: \"stream \"File Name.csv\" \"2021-05-21 20:00:00\"  \"2021-05-21 20:00:00\" ");
            Console.WriteLine("         case 4: opc-stream [csvfile] [-m mappingfile.csv] : maps tags in csv file to new names ");
            Console.WriteLine("             example: \"stream \"File Name.csv\" -m \"Mapping File.csv\" ");
            Console.WriteLine("- read the Readme.md for further instructions");
            Console.WriteLine("----------------------------------------------------------------------");
            var dateStringFormat = ConfigurationManager.AppSettings["TimeStringFormat"];

            string fileName, mappingFile;
            DateTime? startTime, endTime;
            ReadCommandLineOptions(args, dateStringFormat, out fileName, out startTime, out endTime, out mappingFile);

            // if no file name specified, try to get form csv-file
            if (fileName == null || fileName.Length == 0)
            {
                fileName = ConfigurationManager.AppSettings["CsvFile"];
            }
            if (fileName == null || fileName.Length == 0)
            {
                Console.WriteLine("error: no file name specified at command line or in opc-stream.exe.config.Quitting.");
                return;
            }

            OpcStreamer.StreamCSVToOPCDA(fileName, startTime, endTime, mappingFile);
        }

        private static void ReadCommandLineOptions(string[] args, string dateStringFormat, out string fileName, out DateTime? startTime, out DateTime? endTime, out string mappingFile)
        {
            fileName = null;
            startTime = null;
            endTime = null;
            mappingFile = null;
            bool isNextStartTime = false;
            bool isNextEndTime = false;
            bool isNextMappingFile = false;

            // parse through all arguments from command-line
            foreach (var arg in args)
            {
                if (arg.ToLower().Trim() == "opc-stream.exe")
                    continue;

                var argTrim = arg.Trim();

                if (isNextStartTime)
                {
                    isNextStartTime = false;
                    string startTimeStr = argTrim.Replace("\"", "").Trim();
                    startTime = DateTime.ParseExact(startTimeStr, dateStringFormat, CultureInfo.InvariantCulture);
                }
                else if (isNextMappingFile)
                {
                    isNextMappingFile = false;
                    mappingFile = argTrim.Replace("\"", "").Trim();
                }
                else if (isNextEndTime)
                {
                    isNextEndTime = false;
                    string endTimeStr = argTrim.Replace("\"", "").Trim();
                    endTime = DateTime.ParseExact(endTimeStr, dateStringFormat, CultureInfo.InvariantCulture);
                }
                // start time
                else if (argTrim.StartsWith("-s"))
                {
                    isNextStartTime = true;
                }
                // end time
                else if (argTrim.StartsWith("-e"))
                {
                    isNextEndTime = true;
                }
                // mapping file
                else if (argTrim.StartsWith("-m"))
                {
                    isNextMappingFile = true;
                }
                else if (argTrim.EndsWith(".csv"))
                {
                    fileName = argTrim;
                }

            }
        }
    }
}
