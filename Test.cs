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
            OpcStreamer.StreamCSVToOPCDA(@"TestData\minSelect.csv");


        }
    }

}
