using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MumbleSharp.Audio;

namespace MumbleSharpTest
{
    [TestClass]
    public class DynamicCircularBufferTest
    {
        private readonly Random _random = new Random(12352);

        private readonly DynamicCircularBuffer _buffer = new DynamicCircularBuffer(1024);

        [TestMethod]
        public void ConstructingBufferCreatesBufferWithGivenCapacity()
        {
            Assert.AreEqual(1024, _buffer.Capacity);
        }

        [TestMethod]
        public void NewBufferIsEmpty()
        {
            Assert.AreEqual(0, _buffer.Count);
        }

        [TestMethod]
        public void WritingIntoBufferIncreasesCount()
        {
            _buffer.Write(new ArraySegment<byte>(new byte[100]));

            Assert.AreEqual(100, _buffer.Count);
        }

        [TestMethod]
        public void WritingMoreDataIntoBufferGrowsBuffer()
        {
            byte[] b = new byte[1500];
            _random.NextBytes(b);

            _buffer.Write(new ArraySegment<byte>(b));

            Assert.AreEqual(1500, _buffer.Count);
        }

        [TestMethod]
        public void ReadingSmallNumberOfBytesFromBufferIsCorrect()
        {
            byte[] b = new byte[100];
            _random.NextBytes(b);

            _buffer.Write(new ArraySegment<byte>(b));

            byte[] r = new byte[100];
            Assert.AreEqual(100, _buffer.Read(new ArraySegment<byte>(r)));

            for (int i = 0; i < b.Length; i++)
            {
                Assert.AreEqual(b[i], r[i]);
            }
        }

        [TestMethod]
        public void ReadingLargeNumberOfBytesFromBufferIsCorrect()
        {
            byte[] b = new byte[1500];
            _random.NextBytes(b);

            _buffer.Write(new ArraySegment<byte>(b));

            byte[] r = new byte[1500];
            Assert.AreEqual(1500, _buffer.Read(new ArraySegment<byte>(r)));

            for (int i = 0; i < b.Length; i++)
            {
                Assert.AreEqual(b[i], r[i]);
            }
        }

        [TestMethod]
        public void ReadingBytesFromTwistedArrayIsCorrect()
        {
            //Write enough bytes to nearly fill the buffer
            _buffer.Write(new ArraySegment<byte>(new byte[900]));

            //Read most of the bytes back
            Assert.AreEqual(800, _buffer.Read(new ArraySegment<byte>(new byte[800])));

            //Write bytes to wraparound
            byte[] b = new byte[400];
            _random.NextBytes(b);
            _buffer.Write(new ArraySegment<byte>(b));

            //Read them back
            byte[] r = new byte[500];
            Assert.AreEqual(500, _buffer.Read(new ArraySegment<byte>(r)));

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(0, r[i]);

            for (int i = 0; i < b.Length; i++)
                Assert.AreEqual(b[i], r[i + 100]);
        }
    }
}
