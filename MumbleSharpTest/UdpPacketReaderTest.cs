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

        private static byte B(string s)
        {
            return Convert.ToByte(s, 2);
        }

        private static UdpPacketReader R(params byte[] bytes)
        {
            return new UdpPacketReader(new MemoryStream(bytes));
        }

        [TestMethod]
        public void VariableLength_ZeroLeadingOnes()
        {
            var expected = B("01001001");

            UdpPacketReader r = R(expected, B("11111111"));

            var actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void VariableLength_OneLeadingOne()
        {
            var expected = B("00101100") << 8 | B("11100101");

            UdpPacketReader r = R(B("10101100"), B("11100101"), B("11111111"));

            var actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void VariableLength_TwoLeadingOnes()
        {
            var expected = B("00010010") << 16 | B("11100101") << 8 | B("11111111");

            UdpPacketReader r = R(B("11010010"), B("11100101"), B("11111111"));

            var actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void VariableLength_ThreeLeadingOnes()
        {
            var expected = B("00000010") << 24 | B("11100101") << 16 | B("11111111") << 8 | B("00001111");

            UdpPacketReader r = R(B("11100010"), B("11100101"), B("11111111"), B("00001111"));

            var actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void VariableLength_FourLeadingOnes_FourBytes()
        {
            var expected = B("11100101") << 24 | B("11111111") << 16 | B("00001111") << 8 | B("10101010");

            UdpPacketReader r = R(B("11110010"), B("11100101"), B("11111111"), B("00001111"), B("10101010"));

            var actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void VariableLength_FourLeadingOnes_EightBytes()
        {
            var expected = B("11100101") << 56 | B("11111111") << 48 | B("00001111") << 40 | B("10101010") << 32 | B("11100101") << 24 | B("11111111") << 16 | B("00001111") << 8 | B("10101010");

            UdpPacketReader r = R(B("11110110"), B("11100101"), B("11111111"), B("00001111"), B("10101010"), B("11100101"), B("11111111"), B("00001111"), B("10101010"));

            var actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void VariableLength_Negative()
        {
            var expected = ~(B("11100101") << 24 | B("10000001") << 16 | B("00001111") << 8 | B("10101010"));

            UdpPacketReader r = R(B("11111000"), B("11110010"), B("11100101"), B("10000001"), B("00001111"), B("10101010"));

            var actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void VariableLength_InvertedTwoBitNumber()
        {
            //These are 2 bit numbers, test all 4 possibilities

            var expected = ~B("00000000");
            var r = R(B("11111100"));
            var actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);

            expected = ~B("00000001");
            r = R(B("11111101"));
            actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);

            expected = ~B("00000010");
            r = R(B("11111110"));
            actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);

            expected = ~B("00000011");
            r = R(B("11111111"));
            actual = r.ReadVarInt64();
            Assert.AreEqual(expected, actual);
        }
    }
}
