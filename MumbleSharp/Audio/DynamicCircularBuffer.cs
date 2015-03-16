#region Copyright & License
/*************************************************************************
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2014 Roman Atachiants (kelindar@gmail.com)
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
*************************************************************************/
#endregion

using System;

//https://github.com/Kelindar/circular-buffer/blob/master/Source/ByteQueue.cs

namespace MumbleSharp.Audio
{
    /// <summary>
    /// Defines a class that represents a resizable circular byte queue.
    /// </summary>
    public sealed class DynamicCircularBuffer
    {
        // Private fields
        private int _head;
        private int _tail;
        private int _size;
        private byte[] _buffer;

        /// <summary>
        /// Gets the length of the byte queue
        /// </summary>
        public int Count
        {
            get { return _size; }
        }

        public int Capacity
        {
            get { return _buffer.Length; }
        }

        /// <summary>
        /// Constructs a new instance of a byte queue.
        /// </summary>
        public DynamicCircularBuffer(int capacity = 2048)
        {
            _buffer = new byte[capacity];
        }

        /// <summary>
        /// Extends the capacity of the bytequeue
        /// </summary>
        private void SetCapacity(int capacity)
        {
            byte[] newBuffer = new byte[capacity];

            if (_size > 0)
            {
                if (_head < _tail)
                {
                    Buffer.BlockCopy(_buffer, _head, newBuffer, 0, _size);
                }
                else
                {
                    Buffer.BlockCopy(_buffer, _head, newBuffer, 0, _buffer.Length - _head);
                    Buffer.BlockCopy(_buffer, 0, newBuffer, _buffer.Length - _head, _tail);
                }
            }

            _head = 0;
            _tail = _size;
            _buffer = newBuffer;
        }


        /// <summary>
        /// Enqueues a buffer to the queue and inserts it to a correct position
        /// </summary>
        internal void Write(ArraySegment<byte> write)
        {
            var size = write.Count;
            var buffer = write.Array;
            var offset = write.Offset;

            if (size == 0)
                return;

            lock (this)
            {
                if ((_size + size) > _buffer.Length)
                    SetCapacity((_size + size + 2047) & ~2047);

                if (_head < _tail)
                {
                    int rightLength = (_buffer.Length - _tail);

                    if (rightLength >= size)
                    {
                        Buffer.BlockCopy(buffer, offset, _buffer, _tail, size);
                    }
                    else
                    {
                        Buffer.BlockCopy(buffer, offset, _buffer, _tail, rightLength);
                        Buffer.BlockCopy(buffer, offset + rightLength, _buffer, 0, size - rightLength);
                    }
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, _buffer, _tail, size);
                }

                _tail = (_tail + size) % _buffer.Length;
                _size += size;
            }
        }

        /// <summary>
        /// read from the queue
        /// </summary>
        /// <param name="read"></param>
        /// <returns>Number of bytes dequeued</returns>
        internal int Read(ArraySegment<byte> read)
        {
            var size = read.Count;
            var buffer = read.Array;
            var offset = read.Offset;

            lock (this)
            {
                if (size > _size)
                    size = _size;

                if (size == 0)
                    return 0;

                if (_head < _tail)
                {
                    Buffer.BlockCopy(_buffer, _head, buffer, offset, size);
                }
                else
                {
                    int rightLength = (_buffer.Length - _head);

                    if (rightLength >= size)
                    {
                        Buffer.BlockCopy(_buffer, _head, buffer, offset, size);
                    }
                    else
                    {
                        Buffer.BlockCopy(_buffer, _head, buffer, offset, rightLength);
                        Buffer.BlockCopy(_buffer, 0, buffer, offset + rightLength, size - rightLength);
                    }
                }

                _head = (_head + size) % _buffer.Length;
                _size -= size;

                if (_size == 0)
                {
                    _head = 0;
                    _tail = 0;
                }

                return size;
            }
        }
    }

}