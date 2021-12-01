using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace opc_stream
{
    [TestFixture]
    class Test
    {

        [Test]
        public void RunTestData()
        {
            try
            {
                OpcStreamer.StreamCSVToOPCDA(@"C:\Appl\source\opc-stream\TestData\minSelect.csv");
            }
            catch (Exception e)
            {
                Console.WriteLine("exception caught:" + e.ToString());
            }

        }

        [Test]
        public void CommandLine()
        {
            var args = new List<string>();

            try
            {
                Program.Main(args.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine("exception caught:" + e.ToString());
            }
        }

        [Test]
        public void CreateSystemTimeDouble()
        {
            var date = DateTime.Now;

            double test1 = OpcStreamer.CreateSystemTimeDouble(new DateTime(2021,11,23,15,10,20,DateTimeKind.Local));
            double expected = 44523.5905092593;
            double diff = test1 - expected;
            Assert.IsTrue(0.999 < diff && diff < 1.001); 

          /*  double test2 = OpcStreamer.CreateSystemTimeDouble(date.AddSeconds(1));
            double diff = test2 - test1;

            Assert.IsTrue(0.999<diff && diff<1.001);*/

        }

    }

}
