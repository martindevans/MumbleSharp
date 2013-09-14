using System;
using System.Threading;

namespace MumbleSharp
{
    class CryptState
    {
        readonly ReaderWriterLockSlim _aesLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        OcbAes _aes;

        byte[] _serverNonce;

        public byte[] ServerNonce
        {
            get
            {
                try
                {
                    _aesLock.EnterReadLock();
                    return (byte[])_serverNonce.Clone();
                }
                finally
                {
                    _aesLock.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    _aesLock.EnterWriteLock();
                    _serverNonce = value;
                }
                finally
                {
                    _aesLock.ExitWriteLock();
                }
            }
        }

        byte[] _clientNonce;
        public byte[] ClientNonce
        {
            get
            {
                try
                {
                    _aesLock.EnterReadLock();
                    return _clientNonce;
                }
                finally
                {
                    _aesLock.ExitReadLock();
                }
            }
            private set
            {
                try
                {
                    _aesLock.EnterWriteLock();
                    _clientNonce = value;
                }
                finally
                {
                    _aesLock.ExitWriteLock();
                }
            }
        }

        readonly byte[] _decryptHistory = new byte[256];

        public int Good { get; private set; }
        public int Late { get; private set; }
        public int Lost { get; private set; }

        public void SetKeys(byte[] key, byte[] clientNonce, byte[] serverNonce)
        {
            try
            {
                _aesLock.EnterWriteLock();

                _aes = new OcbAes();
                _aes.Initialise(key);

                ServerNonce = serverNonce;
                ClientNonce = clientNonce;
            }
            finally
            {
                _aesLock.ExitWriteLock();
            }
        }

        public byte[] Decrypt(byte[] source, int length)
        {
            try
            {
                _aesLock.EnterReadLock();

                if (length < 4)
                    return null;

                int plainLength = length - 4;

                byte[] saveiv = new byte[OcbAes.BLOCK_SIZE];
                short ivbyte = (short)(source[0] & 0xFF);
                bool restore = false;
                byte[] tag = new byte[OcbAes.BLOCK_SIZE];

                int lost = 0;
                int late = 0;

                Array.ConstrainedCopy(ServerNonce, 0, saveiv, 0, OcbAes.BLOCK_SIZE);

                if (((ServerNonce[0] + 1) & 0xFF) == ivbyte)
                {
                    // In order as expected.
                    if (ivbyte > ServerNonce[0])
                    {
                        ServerNonce[0] = (byte)ivbyte;
                    }
                    else if (ivbyte < ServerNonce[0])
                    {
                        ServerNonce[0] = (byte)ivbyte;
                        for (int i = 1; i < OcbAes.BLOCK_SIZE; i++)
                        {
                            if ((++ServerNonce[i]) != 0)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // This is either out of order or a repeat.
                    int diff = ivbyte - ServerNonce[0];
                    if (diff > 128)
                    {
                        diff = diff - 256;
                    }
                    else if (diff < -128)
                    {
                        diff = diff + 256;
                    }

                    if ((ivbyte < ServerNonce[0]) && (diff > -30) && (diff < 0))
                    {
                        // Late packet, but no wraparound.
                        late = 1;
                        lost = -1;
                        ServerNonce[0] = (byte)ivbyte;
                        restore = true;
                    }
                    else if ((ivbyte > ServerNonce[0]) && (diff > -30) &&
                             (diff < 0))
                    {
                        // Last was 0x02, here comes 0xff from last round
                        late = 1;
                        lost = -1;
                        ServerNonce[0] = (byte)ivbyte;
                        for (int i = 1; i < OcbAes.BLOCK_SIZE; i++)
                        {
                            if ((ServerNonce[i]--) != 0)
                            {
                                break;
                            }
                        }
                        restore = true;
                    }
                    else if ((ivbyte > ServerNonce[0]) && (diff > 0))
                    {
                        // Lost a few packets, but beyond that we're good.
                        lost = ivbyte - ServerNonce[0] - 1;
                        ServerNonce[0] = (byte)ivbyte;
                    }
                    else if ((ivbyte < ServerNonce[0]) && (diff > 0))
                    {
                        // Lost a few packets, and wrapped around
                        lost = 256 - ServerNonce[0] + ivbyte - 1;
                        ServerNonce[0] = (byte)ivbyte;
                        for (int i = 1; i < OcbAes.BLOCK_SIZE; i++)
                        {
                            if ((++ServerNonce[i]) != 0)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }

                    if (_decryptHistory[ServerNonce[0]] == ClientNonce[0])
                    {
                        Array.ConstrainedCopy(saveiv, 0, ServerNonce, 0, OcbAes.BLOCK_SIZE);
                        return null;
                    }
                }

                byte[] dst = _aes.Decrypt(source, 4, plainLength, ServerNonce, 0, source, 0);

                if (tag[0] != source[1] || tag[1] != source[2] || tag[2] != source[3])
                {
                    Array.ConstrainedCopy(saveiv, 0, ServerNonce, 0, OcbAes.BLOCK_SIZE);
                    return null;
                }
                _decryptHistory[ServerNonce[0]] = ServerNonce[1];

                if (restore)
                {
                    Array.ConstrainedCopy(saveiv, 0, ServerNonce, 0, OcbAes.BLOCK_SIZE);
                }

                Good++;
                Late += late;
                Lost += lost;

                return dst;
            }
            finally
            {
                _aesLock.ExitReadLock();
            }
        }
    }
}
