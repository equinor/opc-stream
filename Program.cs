using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using TimeSeriesAnalysis;

using Hylasoft.Opc;

namespace opc_stream
{
    class Program
    {
        static void Main(string[] args)
        {
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
