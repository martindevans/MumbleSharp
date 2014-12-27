using System.Linq;
using MumbleSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace MumbleSharpTest
{
    
    
    /// <summary>
    ///This is a test class for UdpPacketReaderTest and is intended
    ///to contain all UdpPacketReaderTest Unit Tests
    ///</summary>
    [TestClass]
    public class UdpPacketReaderTest
    {
        [TestMethod]
        public void LeadingOnesInAllBytes()
        {
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                var digits = Convert.ToString(i, 2);
                if (digits.Length < 8)
                    digits = Enumerable.Repeat("0", 8 - digits.Length).Aggregate((a, b) => a + b) + digits;

                var expected = digits.TakeWhile(a => a == '1').Count();
                var actual = UdpPacketReader.LeadingOnes((byte)i);
                Assert.AreEqual(expected, actual);
            }

            
        }
    }
}
