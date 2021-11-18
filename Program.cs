using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;


using Hylasoft.Opc;

namespace opc_stream
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("opc-stream - reads time-stamped tags from a csv-file into an OPC-server ");
            Console.WriteLine("     usage:  ");
            Console.WriteLine("         case 1: opc-stream : all configuration is read from opc-stream.exe.config ");
            Console.WriteLine("         case 2: opc-stream [csvfile]: streams specified file, all other configuration from opc-stream.exe.config ");
            Console.WriteLine("             example: \"stream FileName.csv\"");
 /*           Console.WriteLine("         case 3: opc-stream [csvfile] [-s startTime] [-e endTime] : streams specified between two given timestamps ");
            Console.WriteLine("             example: \"stream \"File Name.csv\" \"2021-05-21 20:00:00\"  \"2021-05-21 20:00:00\" ");
            Console.WriteLine("         case 3: opc-stream [csvfile] [-m mappingfile.csv] : maps tags in csv file to new names ");
            Console.WriteLine("             example: \"stream \"File Name.csv\" - m \"Mapping File.csv\" ");
  */          Console.WriteLine("----------------------------------------------------------------------");
            string fileName;
            if (args.Count() > 0)
                fileName = args[0];
            else
            {
                fileName = ConfigurationManager.AppSettings["CsvFile"];
            }
            OpcStreamer.StreamCSVToOPCDA(fileName);
        }
    }
}
