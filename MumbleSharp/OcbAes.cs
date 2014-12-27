using System;
using System.Security.Cryptography;

namespace MumbleSharp
{
    public class OcbAes
    {
        const int PRE_COMP_BLOCKS = 32;
        public const int BLOCK_SIZE = 16;
        public const int TAG_LENGTH = BLOCK_SIZE;

        byte[][] L = new byte[PRE_COMP_BLOCKS][];
        byte[] L_inv = new byte[BLOCK_SIZE];
        byte[] offset = new byte[BLOCK_SIZE]; // Offset (Z[i]) for current block
        byte[] chksum = new byte[TAG_LENGTH]; // Checksum for computing tag
        byte[] tmp = new byte[BLOCK_SIZE];

        RijndaelManaged aes;

        public void Initialise(byte[] key)
        {
            aes = new RijndaelManaged {
                BlockSize = BLOCK_SIZE * 8,
                Key = key,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };

            for (int i = 0; i < L.Length; i++)
                L[i] = new byte[BLOCK_SIZE];
            byte[] L0 = L[0];

            // Precompute L[i]-values (L[0] is synonym of L)
            using (var encryptor = aes.CreateEncryptor())
            {
                encryptor.TransformBlock(L0, 0, L0.Length, L0, 0);
                for (int i = 1; i < PRE_COMP_BLOCKS; i++)
                {
                    // L[i] = L[i - 1] * x
                    byte[] L_cur = L[i], L_pre = L[i - 1];
                    for (int t = 0; t < BLOCK_SIZE - 1; t++)
                        L_cur[t] = (byte)((L_pre[t] << 1) | ((L_pre[t + 1] >> 7) & 1));
                    L_cur[BLOCK_SIZE - 1] = (byte)((L_pre[BLOCK_SIZE - 1] << 1) ^ ((L_pre[0] & 0x80) != 0 ? 0x87 : 0));
                }
            }

            // Precompute L_inv = L / x = L * x^{-1}
            for (int i = BLOCK_SIZE - 1; i > 0; i--)
                L_inv[i] = (byte)(((L0[i] & 0xff) >> 1) | ((L0[i - 1] & 1) << 7));
            L_inv[0] = (byte)((L0[0] & 0xff) >> 1);
            if ((L0[BLOCK_SIZE - 1] & 1) != 0)
            {
                L_inv[0] ^= 0x80;
                L_inv[BLOCK_SIZE - 1] ^= 0x43;
            }
        }

        public void Pmac(byte[] data, int dataPos, int dataLen, byte[] tag, int tagPos)
        {
            if (aes == null)
            {
                throw new InvalidOperationException("AES key not initialized");
            }
            if (data == null || dataPos < 0 || dataLen < 0 || data.Length - dataPos < dataLen)
            {
                throw new ArgumentException("Missing or invalid data");
            }

            Array.Clear(chksum, 0, chksum.Length);
            Array.Clear(offset, 0, offset.Length);

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                // Process blocks 1 .. m-1.   
                for (int i = 1; dataLen > BLOCK_SIZE; i++)
                {
                    XorBlock(offset, offset, L[CountTrailingZeros(i)]); // Update the offset (Z[i] from Z[i-1])
                    XorBlock(tmp, offset, data, dataPos);               // xor input block with Z[i]
                    encryptor.TransformBlock(tmp, 0, tmp.Length, tmp, 0);

                    // Update checksum and the loop variables
                    XorBlock(chksum, chksum, tmp);
                    dataPos += BLOCK_SIZE;
                    dataLen -= BLOCK_SIZE;
                }

                // Process block m
                if (dataLen == BLOCK_SIZE) // full final block
                {
                    XorBlock(chksum, chksum, data, dataPos);
                    XorBlock(chksum, chksum, L_inv);
                }
                else // short final block
                {
                    Array.Clear(tmp, 0, tmp.Length);
                    Array.ConstrainedCopy(data, dataPos, tmp, 0, dataLen);
                    tmp[dataLen] = (byte)0x80;
                    XorBlock(chksum, chksum, tmp);
                }
                encryptor.TransformBlock(chksum, 0, chksum.Length, tmp, 0);
                Array.ConstrainedCopy(tmp, 0, tag, tagPos, TAG_LENGTH);
            }
        }

        protected static int CountTrailingZeros(int i)
        {
            int result = 0;
            while ((i & 1) == 0)
            {
                i >>= 1;
                result++;
            }
            return result;
        }

        protected static void XorBlock(byte[] dst, byte[] src1, byte[] src2)
        {
            for (int i = 0; i < BLOCK_SIZE; i++)
                dst[i] = (byte)(src1[i] ^ src2[i]);
        }

        protected static void XorBlock(byte[] dst, int pos, byte[] src1, byte[] src2)
        {
            for (int i = 0; i < BLOCK_SIZE; i++)
                dst[pos + i] = (byte)(src1[i] ^ src2[i]);
        }

        protected static void XorBlock(byte[] dst, byte[] src1, byte[] src2, int pos)
        {
            for (int i = 0; i < BLOCK_SIZE; i++)
                dst[i] = (byte)(src1[i] ^ src2[pos + i]);
        }

        /// <summary>
        /// Given a nonce starting at offset noncePos of the nonce byte array, encrypt ptLen elements of byte array pt starting at offset ptPos, and producing a tag starting at offset tagPos of the tag byte array.
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="ptPos"></param>
        /// <param name="ptLen"></param>
        /// <param name="nonce"></param>
        /// <param name="noncePos"></param>
        /// <param name="tag"></param>
        /// <param name="tagPos"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] pt, int ptPos, int ptLen, byte[] nonce, int noncePos, byte[] tag, int tagPos)
        {
            if (aes == null)
                throw new InvalidOperationException("AES key not initialized");
            if (pt == null || ptPos < 0 || ptLen < 0 || pt.Length - ptPos < ptLen)
                throw new ArgumentException("Missing or invalid plaintext");
            if (nonce == null || noncePos < 0 || nonce.Length - noncePos < BLOCK_SIZE)
                throw new ArgumentException("Missing or invalid nonce");
            if (tag == null || tagPos < 0 || tag.Length - tagPos < TAG_LENGTH)
                throw new ArgumentException("Missing or invalid tag");

            // Create ciphertext
            byte[] ct = new byte[ptLen];
            int ctPos = 0;

            Array.Clear(chksum, 0, chksum.Length);
            XorBlock(offset, L[0], nonce, noncePos);            // Calculate R, aka Z[0]

            using (var encryptor = aes.CreateEncryptor())
            {
                encryptor.TransformBlock(offset, 0, offset.Length, offset, 0);

                // Process blocks 1 .. m-1
                int i;
                for (i = 1; ptLen > BLOCK_SIZE; i++)
                {
                    XorBlock(chksum, chksum, pt, ptPos);            // Update the checksum
                    XorBlock(offset, offset, L[CountTrailingZeros(i)]);            // Update the offset (Z[i] from Z[i-1])
                    XorBlock(tmp, offset, pt, ptPos);               // xor the plaintext block with Z[i]
                    encryptor.TransformBlock(tmp, 0, tmp.Length, tmp, 0);// Encipher the block
                    XorBlock(ct, ctPos, offset, tmp);               // xor Z[i] again, writing result to ciphertext pointer
                    ptLen -= BLOCK_SIZE;
                    ptPos += BLOCK_SIZE;
                    ctPos += BLOCK_SIZE;
                }

                // Process block m
                XorBlock(offset, offset, L[CountTrailingZeros(i)]);                // Update the offset (Z[m] from Z[m-1])
                XorBlock(tmp, offset, L_inv);                       // xor L . x^{-1} and Z[m]
                tmp[BLOCK_SIZE - 1] ^= (byte)(ptLen << 3);          // Add in final block bit-length
                encryptor.TransformBlock(tmp, 0, tmp.Length, tmp, 0);

                for (int t = 0; t < ptLen; t++)
                {
                    ct[ctPos + t] = (byte)(pt[ptPos + t] ^ tmp[t]); // xor pt with block-cipher output to get ct
                    tmp[t] = pt[ptPos + t];                         // Add to checksum the ptLen bytes of plaintext...
                }
                XorBlock(chksum, chksum, tmp);                      // ... followed by the last (16 - ptLen) bytes of block-cipher output

                // Calculate tag
                XorBlock(chksum, chksum, offset);
                encryptor.TransformBlock(chksum, 0, tmp.Length, tmp, 0);
                Array.ConstrainedCopy(tmp, 0, tag, tagPos, TAG_LENGTH);

                return ct;
            }
        }

        /// <summary>
        /// Given a nonce starting at offset noncePos of the nonce byte array,
        /// decrypt cipherTextLength elements of byte array cipherText starting at offset cipherTextPosition, and
        /// verifying a tag starting at offset tagPos of the tag byte array.
        /// </summary>
        /// <param name="cipherText">ciphertext buffer.</param>
        /// <param name="cipherTextPosition">start index of ciphertext on cipherText.</param>
        /// <param name="cipherTextLength">byte length of ciphertext.</param>
        /// <param name="nonce">nonce array (BLOCK_SIZE bytes).</param>
        /// <param name="noncePos">start index of nonce.</param>
        /// <param name="tag">input tag buffer.</param>
        /// <param name="tagPos">start index of tag.</param>
        /// <returns>the resulting plaintext, a byte array of same length as ct
        /// if decryption is successfull, or else null if the tag does not correctly verify.</returns>
        public byte[] Decrypt(byte[] cipherText, int cipherTextPosition, int cipherTextLength, byte[] nonce, int noncePos, byte[] tag, int tagPos)
        {
            if (aes == null)
                throw new InvalidOperationException("AES key not initialized");
            if (cipherText == null || cipherTextPosition < 0 || cipherTextLength < 0 || cipherText.Length - cipherTextPosition < cipherTextLength)
                throw new ArgumentException("Missing or invalid ciphertext");
            if (nonce == null || noncePos < 0 || nonce.Length - noncePos < BLOCK_SIZE)
                throw new ArgumentException("Missing or invalid nonce");
            if (tag == null || tagPos < 0 || tag.Length - tagPos < TAG_LENGTH)
                throw new ArgumentException("Missing or invalid tag");

            // Create plaintext
            byte[] pt = new byte[cipherTextLength];
            int ptPos = 0;

            Array.Clear(chksum, 0, chksum.Length);
            XorBlock(offset, L[0], nonce, noncePos);    // Calculate R, aka Z[0]

            using (var encryptor = aes.CreateEncryptor())
            {
                using (var decryptor = aes.CreateDecryptor())
                {
                    encryptor.TransformBlock(offset, 0, offset.Length, offset, 0);

                    // Process blocks 1 .. m-1
                    int i;
                    for (i = 1; cipherTextLength > BLOCK_SIZE; i++)
                    {
                        XorBlock(offset, offset, L[CountTrailingZeros(i)]); // Update the offset (Z[i] from Z[i-1])
                        XorBlock(tmp, offset, cipherText, cipherTextPosition);               // xor ciphertext block with Z[i]
                        decryptor.TransformBlock(tmp, 0, tmp.Length, tmp, 0);// Decipher the next block-cipher block
                        XorBlock(pt, ptPos, offset, tmp);               // xor Z[i] again, writing result to plaintext pointer
                        XorBlock(chksum, chksum, pt, ptPos);            // Update the checksum
                        cipherTextLength -= BLOCK_SIZE;
                        cipherTextPosition += BLOCK_SIZE;
                        ptPos += BLOCK_SIZE;
                    }

                    // Process block m
                    XorBlock(offset, offset, L[CountTrailingZeros(i)]);                // Update the offset (Z[m] from Z[m-1])
                    XorBlock(tmp, offset, L_inv);                       // xor L . x^{-1} and Z[m]
                    tmp[BLOCK_SIZE - 1] ^= (byte)(cipherTextLength << 3);          // Add in final block bit-length
                    encryptor.TransformBlock(tmp, 0, tmp.Length, tmp, 0);

                    for (int t = 0; t < cipherTextLength; t++)
                    {
                        pt[ptPos + t] = (byte)(cipherText[cipherTextPosition + t] ^ tmp[t]); // xor ct with block-cipher output to get pt
                        tmp[t] = pt[ptPos + t];                         // Add to checksum the ctLen bytes of plaintext...
                    }
                    XorBlock(chksum, chksum, tmp);                      // ... followed by the last (16 - ptLen) bytes of block-cipher output

                    // Calculate and verify tag
                    XorBlock(chksum, chksum, offset);
                    encryptor.TransformBlock(chksum, 0, chksum.Length, tmp, 0);
                    for (int t = 0; t < TAG_LENGTH; t++)
                    {
                        if (tmp[t] != tag[tagPos + t])
                        {
                            Array.Clear(pt, 0, pt.Length);
                            pt = null;
                            break;
                        }
                    }

                    return pt;
                }
            }
        }
    }
}
