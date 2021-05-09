/// 컴컴컴컴컴컴컴컴?
/// CRC32(TPU7-RM)   (c)2001 NNCI
/// 컴컴컴컴컴컴컴컴?
/// A low level unit for CRC32 calculation on every kind of data

using System;

namespace WiseUnpacker.Inflation
{
    internal class CRC32
    {
        private const long seed = 0xedb88320;
        private uint[] crcTable = new uint[0x100];

        public uint Value { get; set; }

        public CRC32()
        {
            BuildTable();
            Value = UInt32.MaxValue;
        }

        public void Finalize()
        {
            Value = ~Value;
        }

        public void Update(byte B)
        {
            Value = crcTable[(byte)(Value ^ B)] ^ (Value >> 8);
        }

        public void Update(byte[] buffer, int index, int length)
        {
            if (length <= 0)
                return;

            for (int i = index; i < length; i++)
            {
                Update(buffer[i]);
            }
        }

        private void BuildTable()
        {
            for (int W0 = 0; W0 < 256; W0++)
            {
                uint tempCrc = (uint)(W0 << 1);
                for (int W1 = 8; W1 >= 0; W1--)
                {
                    if ((tempCrc & 1) == 1)
                        tempCrc = (uint)((tempCrc >> 1) ^ seed);
                    else
                        tempCrc = tempCrc >> 1;
                }

                crcTable[W0] = tempCrc;
            }
        }
    }
}
