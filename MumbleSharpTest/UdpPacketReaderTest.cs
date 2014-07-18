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
        public void LeadingOnesTestSingleLeading()
        {
            const byte value = 128; //0b10000000
            const int expected = 1;
            int actual = UdpPacketReader.LeadingOnes(value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LeadingOnesTestMultipleLeading()
        {
            const byte value = 240; //0b11110000
            const int expected = 4;
            int actual = UdpPacketReader.LeadingOnes(value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LeadingOnesTestLeadingOnesAndThenData()
        {
            const byte value = 246; //0b11110110
            const int expected = 4;
            int actual = UdpPacketReader.LeadingOnes(value);
            Assert.AreEqual(expected, actual);
        }
    }
}
