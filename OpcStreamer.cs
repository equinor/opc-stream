using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

using System.Configuration;

using Hylasoft.Opc.Common;
using Hylasoft.Opc.Da;

namespace opc_stream
{
    class OpcStreamer
    {
        public static bool StreamCSVToOPCDA(string fileName)
        {
            Console.WriteLine("Trying to read opc-stream.exe.config....");
            char separator = (char)(ConfigurationManager.AppSettings["CSVSeparator"].Trim().First());
            var dateformat = ConfigurationManager.AppSettings["TimeStringFormat"];

            Console.WriteLine("Trying to read file:"+ fileName);

            var csv = new CsvLineReader(fileName,separator,dateformat);
            var firstLine = csv.GetNextLine();

        //    var dataSet = new TimeSeriesDataSet(fileName, separator, dateformat);

            // start by creating a list of all the variable names of the opc server
            // on the format:
            // Name = "26FIC1111_SPE"
            //Value = 22150
            //Type = float

            string timeIntegerName = "_Time_Seconds"; // number between 0-60 showing the seconds
            string systemTimeName = "_Time_System";// number of second since "01/01/1970"

            // write a "taglist" - that can be used to set up "Statoi.OPC.Server"
            using (var fileObj = new StringToFileWriter("opc-stream-taglist.txt"))
            {

                fileObj.Write("Name= \"" + timeIntegerName + "\"\r\n");
                fileObj.Write("Value= " + 0 + "\r\n");
                fileObj.Write("Type= int" + "\r\n");
                fileObj.Write("\r\n");

                fileObj.Write("Name= \"" + systemTimeName + "\"\r\n");
                fileObj.Write("Value= " + CreateSystemTimeDouble(firstLine.Item1).ToString() + "\r\n");
                fileObj.Write("Type= float" + "\r\n");
                fileObj.Write("\r\n");

                int k = 0;
                foreach (var variable in csv.GetVariableNames())
                {
                    if (variable == "Time")
                        continue;
                    fileObj.Write("Name= \"" + variable+"\"\r\n");
                    fileObj.Write("Value= " + firstLine.Item2[k].ToString() + "\r\n");
                    fileObj.Write("Type= float" + "\r\n");
                    fileObj.Write("\r\n");
                    k++;
                }
                fileObj.Close();
             }
            Console.WriteLine("Wrote:" + csv.GetVariableNames().Length +"tagnames to opc-stream-taglist.txt ");

            // connect to OPC DA server
            string serverUrl = ConfigurationManager.AppSettings["DaOpcServerURI"];
            Console.WriteLine("Connecting to:" + serverUrl + "...");
            bool doVerboseOutput = ConfigurationManager.AppSettings["BeVerbose"]== "1"? true:false;

            long totalRunTime_ms = 0;
            long totalWaitTime_ms = 0;
            long timeToSubtractFromEachWait_ms = Convert.ToInt32(ConfigurationManager.AppSettings["TimeToSubtractFromEachWait_ms"]);
            Console.WriteLine (timeToSubtractFromEachWait_ms +"ms subtracted from cacluated wait time at each iteration");


            using (var client = new DaClient(new Uri(@"opcda://"+serverUrl)))
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
                // repeat until end-of-file found
                while (nextLine.Item2 != null) 
                {
                    prev_elapsedMS = total_timer.ElapsedMilliseconds;
                    // first write the two times
                    double systemTime;
                    int seconds;
                    try
                    {
                        seconds = nextLine.Item1.Second;
                        systemTime = CreateSystemTimeDouble(nextLine.Item1); 

                        client.Write(systemTimeName, systemTime);
                        client.Write(timeIntegerName, seconds);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception writing time-tags "+ timeIntegerName +" or "+ systemTimeName + " at index:" 
                            + curTimeIdx + " : " + e.ToString());
                        Console.ReadLine();
                        return false;
                    }

                    for (int curSignalIdx = 0; curSignalIdx < Math.Min(signalNames.Length, nextLine.Item2.Length); curSignalIdx++)
                    {
                        if (signalNames[curSignalIdx].ToLower() == "time")
                        {
                            continue;
                        }
                        try
                        {
                            client.Write(signalNames[curSignalIdx], nextLine.Item2[curSignalIdx]);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception writing:" + signalNames[curSignalIdx] + " at index:" + curTimeIdx + " : " + e.ToString());
                            Console.ReadLine();
                            return false;
                        }
                    }
                    long elapsedMS_BeforeConsole = total_timer.ElapsedMilliseconds;
                    if (doVerboseOutput)
                    {
                        string curDate = csv.GetTimeStampAtLastLineRead();

                        Console.WriteLine("Wrote index:" + curTimeIdx + " TimeStamp:" + curDate + " "+ systemTimeName+
                            ":" + systemTime + " " + timeIntegerName +":"+ seconds + " in:"+ (elapsedMS_BeforeConsole- prev_elapsedMS) + "ms");
                    }
                    long elapsedMSsnapshot = total_timer.ElapsedMilliseconds;

                    long elapsedMS = elapsedMSsnapshot - prev_elapsedMS;
                  
                    totalRunTime_ms += elapsedMS;
                    long timeToWaitMs = samplingTimeMs - elapsedMS;
                    totalWaitTime_ms += timeToWaitMs;
                    if (timeToWaitMs > 0)
                    {
                        Thread.Sleep((int)(timeToWaitMs- timeToSubtractFromEachWait_ms));
                    }
                    else
                    {
                        nTimingErrors++;
                        Console.WriteLine("WARNING: iteration took more than"+ samplingTimeMs+ "ms to write! :" + elapsedMS);
                    }
                    // finally read next line in prepartion for next iteration.
                    curTimeIdx++;
                    nextLine = csv.GetNextLine();
                }
                total_timer.Stop();
                long totalUnaccountedForTimeMs = total_timer.ElapsedMilliseconds - totalWaitTime_ms - totalRunTime_ms;

                Console.WriteLine("DONE!(reached end-of-file) in " + total_timer.Elapsed.TotalSeconds.ToString("F1") + " sec, verus expected:" 
                    + (curTimeIdx+1)/ (1000/samplingTimeMs)+" sec. ");
                Console.WriteLine(nTimingErrors + " caught timing errors");
                Console.WriteLine("total time run:" + (totalRunTime_ms/1000).ToString("F1")  
                    + "s.total time waited:"+ (totalWaitTime_ms/1000).ToString("F1") + "s");
                Console.WriteLine("unaccounted for time:"+ (totalUnaccountedForTimeMs/1000).ToString("F1") + "s");
            }
            Console.ReadLine();
            return true;
        }


        public static double CreateSystemTimeDouble(DateTime time)
        {
            return (double)(time - new DateTime(1970, 1, 1)).TotalSeconds;
        
        }

    }
}
