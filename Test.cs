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
        public void RunFieldData()
        {
            try
            {
                OpcStreamer.StreamCSVToOPCDA(@"C:\Appl\OneDrive - Equinor\2021_SubseaPALL\2021_12_TestSetup\KristinDataset\KriCache.csv");
            }
            catch (Exception e)
            {
                Console.WriteLine("exception caught:"+e.ToString());
            }

        }

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
        public void CreateSystemTimeDouble()
        {
            var date = DateTime.Now;

            double test1 = OpcStreamer.CreateSystemTimeDouble(DateTime.Now);
            double test2 = OpcStreamer.CreateSystemTimeDouble(date.AddSeconds(1));
            double diff = test2 - test1;

            Assert.IsTrue(0.999<diff && diff<1.001);

        }

    }

}
