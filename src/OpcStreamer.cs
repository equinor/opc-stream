using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.IO;

using System.Configuration;

using Hylasoft.Opc.Common;
using Hylasoft.Opc.Da;

namespace opc_stream
{
    class OpcStreamer
    {
        static string timeSecondsInMinuteIntegerName = "_Time_SecondsInMinute";
        static string timeSecondsInHourIntegerName = "_Time_SecondsInHour";
        static string systemTimeName = "_Time_System";

        public static bool StreamCSVToOPCDA(string fileName, DateTime? startTime=null , DateTime? endTime=null,string mappingFile= null)
        {
            Console.WriteLine("Trying to read opc-stream.exe.config....");
            char separator = (char)(ConfigurationManager.AppSettings["CSVSeparator"].Trim().First());
            var dateformat = ConfigurationManager.AppSettings["TimeStringFormat"];
            //////
            Dictionary<string, string> csvToOpcMappingDict;
            List<int> mappingIdxToColumnIdx;

            var csv = new CsvLineReader(fileName, separator, dateformat);
            ParseMappingFile(csv, mappingFile, separator, out csvToOpcMappingDict, out mappingIdxToColumnIdx);
            //////
            var firstLine = csv.GetNextLine();

            CreateStatoilOPCTagList(csvToOpcMappingDict, csv, firstLine);


            // connect to OPC DA server
            string serverUrl = ConfigurationManager.AppSettings["DaOpcServerURI"];
            Console.WriteLine("Connecting to:" + serverUrl + "...");
            bool doVerboseOutput = ConfigurationManager.AppSettings["BeVerbose"] == "1" ? true : false;

            long totalRunTime_ms = 0;
            long totalWaitTime_ms = 0;
            long timeToSubtractFromEachWait_ms = Convert.ToInt32(ConfigurationManager.AppSettings["TimeToSubtractFromEachWait_ms"]);
            if (timeToSubtractFromEachWait_ms > 0)
            {
                Console.WriteLine(timeToSubtractFromEachWait_ms + "ms subtracted from calcuated wait time at each iteration");
            }

            using (var client = new DaClient(new Uri(@"opcda://" + serverUrl)))
            {
                client.Connect();
                if (client.Status == OpcStatus.NotConnected)
                {
                    Console.WriteLine("!! QUITTING !! - NOT able to connect to server:" + serverUrl + "");
                    Console.WriteLine("Press any key....");
                    Console.ReadLine();
                    return false;
                }
                // write all values, while waiting the appropriate time between iteraitons
                string[] signalNames = csv.GetVariableNames();
                int samplingTimeMs = Convert.ToInt32(ConfigurationManager.AppSettings["SampleTime_ms"]);
                Console.WriteLine("Writing values to OPC-server from CSV-file....");

                Stopwatch looptimer = new Stopwatch();
                Stopwatch total_timer = new Stopwatch();
                int nTimingErrors = 0;

                total_timer.Start();
                long prev_elapsedMS = 0;
                var nextLine = csv.GetNextLine();
                int curTimeIdx = 0;
                bool isSeekingStart = false;
                if (startTime.HasValue)
                {
                    isSeekingStart = true;
                    Console.WriteLine("seeking the specified start-time....");
                }
                // repeat until end-of-file found
                bool isDone = false;
                while (nextLine.Item2 != null && !isDone)
                {
                    if (startTime.HasValue)
                    {
                        if (nextLine.Item1 < startTime)
                        {
                            curTimeIdx++;
                            nextLine = csv.GetNextLine();
                            continue;
                        }
                        if (isSeekingStart)
                        {
                            isSeekingStart = false;
                            Console.WriteLine("successfully seeked out line " + csv.GetCurrentLineNumber() + " of csv before starting stream");
                        }
                    }
                    if (endTime.HasValue)
                    {
                        if (nextLine.Item1 > endTime)
                        {
                            isDone = true;
                            Console.WriteLine("successfully reached line " + csv.GetCurrentLineNumber() + " of csv.stream finished. ");
                            continue;
                        }
                    }

                    prev_elapsedMS = total_timer.ElapsedMilliseconds;

                    // write all signals first, if no mapping
                    if (csvToOpcMappingDict.Count == 0)
                    {
                        for (int curSignalIdx = 0; curSignalIdx < Math.Min(signalNames.Length, nextLine.Item2.Length); curSignalIdx++)
                        {
                            if (signalNames[curSignalIdx].ToLower() == "time")
                            {
                                continue;
                            }
                            try
                            {
                                client.WriteAsync<double>(signalNames[curSignalIdx], nextLine.Item2[curSignalIdx]);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Exception writing:" + signalNames[curSignalIdx] + " at index:" + curTimeIdx + " : " + e.ToString());
                                Console.ReadLine();
                                return false;
                            }
                        }
                    }
                    else
                    {
                        for (int curMapIdx = 0; curMapIdx < Math.Min(csvToOpcMappingDict.Count, mappingIdxToColumnIdx.Count); curMapIdx++)
                        {
                            var mapping = csvToOpcMappingDict.ElementAt(curMapIdx);
                            var opcSignalName = mapping.Value;
                            var valueIndex = mappingIdxToColumnIdx[curMapIdx] - 1;// minus one because first column is time, nextLine.Item2 just gives values.
                            var value = nextLine.Item2[valueIndex];
                            try
                            {
                                client.WriteAsync<double>(opcSignalName, value);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Exception writing:" + opcSignalName + " at index:" + curTimeIdx + " : " + e.ToString());
                                Console.ReadLine();
                                return false;
                            }
                        }
                    }

                    // lastly write time tags
                    double systemTime;
                    int secondsInMinute;
                    int secondsInHour;
                    try
                    {
                        secondsInMinute = nextLine.Item1.Second;
                        secondsInHour = nextLine.Item1.Second + nextLine.Item1.Minute * 60;
                        systemTime = CreateSystemTimeDouble(nextLine.Item1);

                        client.WriteAsync<double>(systemTimeName, systemTime);
                        client.WriteAsync<double>(timeSecondsInMinuteIntegerName, secondsInMinute);
                        client.WriteAsync<double>(timeSecondsInHourIntegerName, secondsInHour);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception writing time-tags " + timeSecondsInMinuteIntegerName + " or " + systemTimeName + " at index:"
                            + curTimeIdx + " : " + e.ToString());
                        Console.ReadLine();
                        return false;
                    }

                    long elapsedMS_BeforeConsole = total_timer.ElapsedMilliseconds;
                    if (doVerboseOutput)
                    {
                        string curDate = csv.GetTimeStampAtLastLineRead();

                        Console.WriteLine("Wrote index:" + curTimeIdx + " TimeStamp:" + curDate + " " + systemTimeName +
                            ":" + systemTime + " " + timeSecondsInMinuteIntegerName + ":" + secondsInMinute + " in:" + (elapsedMS_BeforeConsole - prev_elapsedMS) + "ms");
                    }
                    long elapsedMSsnapshot = total_timer.ElapsedMilliseconds;
                    long elapsedMS = elapsedMSsnapshot - prev_elapsedMS;

                    //totalRunTime_ms += elapsedMS;
                    long timeToWaitMs = samplingTimeMs - elapsedMS - timeToSubtractFromEachWait_ms;
                    totalWaitTime_ms += timeToWaitMs;
                    if (timeToWaitMs > 0)
                    {
                        Thread.Sleep((int)(timeToWaitMs));
                    }
                    else
                    {
                        nTimingErrors++;
                        Console.WriteLine("WARNING: at line " + curTimeIdx + " iteration took more than " + samplingTimeMs + " ms to write! :" + elapsedMS);
                    }
                    // finally read next line in prepartion for next iteration.
                    curTimeIdx++;
                    nextLine = csv.GetNextLine();
                }
                total_timer.Stop();
                long totalUnaccountedForTimeMs = total_timer.ElapsedMilliseconds - totalWaitTime_ms - totalRunTime_ms;

                Console.WriteLine("DONE!(reached end-of-file) in " + total_timer.Elapsed.TotalSeconds.ToString("F1") + " sec, verus expected:"
                    + (curTimeIdx + 1) / (1000 / samplingTimeMs) + " sec. ");
                Console.WriteLine(nTimingErrors + " caught timing errors");
                Console.WriteLine(/*"total time run:" + (totalRunTime_ms/1000).ToString("F1")  
                    +*/ "s.total time waited:" + (totalWaitTime_ms / 1000).ToString("F1") + "s");
                //  Console.WriteLine("unaccounted for time:"+ (totalUnaccountedForTimeMs/1000).ToString("F1") + "s");
            }
            Console.ReadLine();
            return true;
        }

        private static void CreateStatoilOPCTagList(Dictionary<string, string> csvToOpcMappingDict, CsvLineReader csv, (DateTime, double[]) firstLine)
        {

            // start by creating a list of all the variable names of the opc server
            // on the format:
            // Name = "26FIC1111_SPE"
            //Value = 22150
            //Type = float


            // write a "taglist" - that can be used to set up "Statoil.OPC.Server"
            using (var fileObj = new StringToFileWriter("opc-stream-taglist.txt"))
            {

                fileObj.Write("Name= \"" + timeSecondsInMinuteIntegerName + "\"\r\n");
                fileObj.Write("Value= " + 0 + "\r\n");
                fileObj.Write("Type= int" + "\r\n");
                fileObj.Write("\r\n");

                fileObj.Write("Name= \"" + timeSecondsInHourIntegerName + "\"\r\n");
                fileObj.Write("Value= " + 0 + "\r\n");
                fileObj.Write("Type= int" + "\r\n");
                fileObj.Write("\r\n");

                fileObj.Write("Name= \"" + systemTimeName + "\"\r\n");
                fileObj.Write("Value= " + CreateSystemTimeDouble(firstLine.Item1).ToString() + "\r\n");
                fileObj.Write("Type= float" + "\r\n");
                fileObj.Write("\r\n");

                if (csvToOpcMappingDict.Count == 0)
                {
                    int k = 0;
                    foreach (var variable in csv.GetVariableNames())
                    {
                        if (variable == "Time")
                            continue;
                        fileObj.Write("Name= \"" + variable + "\"\r\n");
                        fileObj.Write("Value= " + firstLine.Item2[k].ToString() + "\r\n");
                        fileObj.Write("Type= float" + "\r\n");
                        fileObj.Write("\r\n");
                        k++;
                    }
                    Console.WriteLine("Wrote:" + csv.GetVariableNames().Length + "tagnames to opc-stream-taglist.txt ");
                }
                else
                {
                    int k = 0;
                    foreach (var key in csvToOpcMappingDict.Keys)
                    {
                        fileObj.Write("Name= \"" + csvToOpcMappingDict[key] + "\"\r\n");
                        fileObj.Write("Value= " + 0 + "\r\n");
                        fileObj.Write("Type= float" + "\r\n");
                        fileObj.Write("\r\n");
                        k++;
                    }
                    Console.WriteLine("Wrote:" + csvToOpcMappingDict.Count() + "tagnames to opc-stream-taglist.txt ");
                }
                fileObj.Close();
            }
        }

        private static void ParseMappingFile(CsvLineReader csv, string mappingFile, char separator, out Dictionary<string, string> csvToOpcMappingDict, out List<int> mappingIdxToColumnIdx)
        {
            csvToOpcMappingDict = new Dictionary<string, string>();
            mappingIdxToColumnIdx = new List<int>();
            if (mappingFile != null)
            {
                using (var mappingReader = new StreamReader(mappingFile))
                {

                    while (!mappingReader.EndOfStream)
                    {
                        string currentLine = mappingReader.ReadLine();
                        var splitLine = currentLine.Split(separator);
                        if (splitLine.Count() == 2)
                        {
                            csvToOpcMappingDict.Add(splitLine[0].Trim(), splitLine[1].Trim());
                        }
                        else
                        {
                            Console.WriteLine("error reading mapping file line:" + currentLine);
                            Console.WriteLine("Press any key....");
                            Console.ReadLine();
                        }
                    }
                }
                Console.WriteLine("Read " + csvToOpcMappingDict.Count() + " mappings from file.");

                Console.WriteLine("Trying to read CSV-file:" + csv.GetFileName()) ;


                // figure out which column each mapped tag is in and store in mappingIdxToColumnIdx
                if (csvToOpcMappingDict.Count > 0)
                {
                    var csvVarNames = csv.GetVariableNames();
                    foreach (var mapping in csvToOpcMappingDict)
                    {
                        var variableToLookFor = mapping.Key;
                        int i = 0;
                        bool found = false;
                        while (i < csvVarNames.Length && !found)
                        {
                            if (csvVarNames[i] == variableToLookFor)
                            {
                                found = true;
                                mappingIdxToColumnIdx.Add(i);
                            }
                            i++;
                        }
                        if (!found)
                        {
                            Console.WriteLine("WARNING: variable \"" + variableToLookFor + "\" not found in CSV.");
                            Console.WriteLine("Press any key....");
                            Console.ReadLine();
                        }
                    }
                }

            }
        }

        public static double CreateSystemTimeDouble(DateTime time)
        {
            return (double)(time - new DateTime(1900, 1, 1,0,0,0,DateTimeKind.Utc)).TotalDays;
        }

    }
}
