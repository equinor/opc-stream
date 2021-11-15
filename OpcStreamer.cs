using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;
using System.Configuration;

using Hylasoft.Opc.Common;
using Hylasoft.Opc.Da;

namespace opc_stream
{
    class OpcStreamer
    {
        public static bool StreamCSVToOPCDA(string fileName)
        {
            var dataSet = new TimeSeriesDataSet(fileName);

            // start by creating a list of all the variable names of the opc server
            // on the format:
            // Name = "26FIC1111_SPE"
            //Value = 22150
            //Type = float

            using (var fileObj = new StringToFileWriter("opc-stream-taglist.txt"))
            {
                foreach (var variable in dataSet.GetSignalNames())
                {
                    if (variable == "Time")
                        continue;
                    fileObj.Write("Name= \"" + variable+"\"\r\n");
                    fileObj.Write("Value= " + dataSet.GetValues(variable).First().ToString() + "\r\n");
                    fileObj.Write("Type= float" + "\r\n");
                    fileObj.Write("\r\n");

                }
                fileObj.Close();
             }

            // connect to OPC DA server
            string serverUrl = ConfigurationManager.AppSettings["DaOpcServerURI"];
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
                string[] signalNames = dataSet.GetSignalNames();
                int samplingTimeMs = Convert.ToInt32(ConfigurationManager.AppSettings["SampleTime_ms"]);
                for (int curTimeIdx = 0; curTimeIdx < dataSet.GetLength(); curTimeIdx++)
                {
                    for (int curSignalIdx = 0; curSignalIdx < signalNames.Length; curSignalIdx++)
                    {
                        if (signalNames[curSignalIdx] == "Time")
                            continue;
                        try
                        {
                            client.Write(signalNames[curSignalIdx], dataSet.GetValues(signalNames[curSignalIdx])[curTimeIdx]);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception writing:" + signalNames[curSignalIdx] + " at index:" + curSignalIdx + " : " + e.ToString());
                            Console.ReadLine();
                            return false;
                        }

                    }
                    Console.WriteLine("Wrote index:");
                    Thread.Sleep(samplingTimeMs);
                }
            }

            Console.WriteLine("DONE!");
            return true;
        }

    }
}
