using System;
using System.IO;

namespace WiseUnpacker.Files
{
    internal class ReadOnlyFile
    {
        private readonly Stream stream;
        private byte[] buffer;
        private long bufferOffset;
        private long position;

        public string Name { get; private set; }
        public long Length { get; private set; }

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        private ReadOnlyFile(string name) : this(name, File.OpenRead(name)) { }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReadOnlyFile(string directory, string filename) : this(Path.Combine(directory, filename)) { }

        /// <summary>
        /// Constructor
        /// </summary>
        private ReadOnlyFile(string name, Stream stream)
        {
            this.stream = stream;
            this.Name = name;
            this.buffer = new byte[0x8000];
            this.Length = stream.Length;
            this.bufferOffset = 0xffff0000;
            this.position = 0;
        }

        #endregion

        #region Stream Wrappers

        public void Close()
        {
            buffer = null;
            stream.Close();
        }

        public bool EOF()
        {
            return position == Length;
        }

        public byte ReadByte()
        {
            return ReadByte(position);
        }

        public byte ReadByte(long p)
        {
            byte[] res = new byte[1];
            if (ValidPosition(p))
            {
                if (!InMemory(p, 1))
                    FillBuffer(p);

                Array.ConstrainedCopy(buffer, (int)(p - bufferOffset), res, 0, 1);
                position++;
            }

            return res[0];
        }

        public short ReadInt16()
        {
            return ReadInt16(position);
        }

        public short ReadInt16(long p)
        {
            byte[] res = new byte[2];
            if (ValidPosition(p))
            {
                if (!InMemory(p, 2))
                    FillBuffer(p);

                Array.ConstrainedCopy(buffer, (int)(p - bufferOffset), res, 0, 2);
                position++;
            }

            return BitConverter.ToInt16(res, 0);
        }

        public ushort ReadUInt16()
        {
            return ReadUInt16(position);
        }

        public ushort ReadUInt16(long p)
        {
            byte[] res = new byte[2];
            if (ValidPosition(p))
            {
                if (!InMemory(p, 2))
                    FillBuffer(p);

                Array.ConstrainedCopy(buffer, (int)(p - bufferOffset), res, 0, 2);
                position++;
            }

            return BitConverter.ToUInt16(res, 0);
        }
        
        public int ReadInt32()
        {
            return ReadInt32(position);
        }

        public int ReadInt32(long p)
        {
            byte[] res = new byte[4];
            if (ValidPosition(p))
            {
                if (!InMemory(p, 4))
                    FillBuffer(p);

                Array.ConstrainedCopy(buffer, (int)(p - bufferOffset), res, 0, 4);
                position++;
            }

            return BitConverter.ToInt32(res, 0);
        }

        public uint ReadUInt32()
        {
            return ReadUInt32(position);
        }

        public uint ReadUInt32(long p)
        {
            byte[] res = new byte[4];
            if (ValidPosition(p))
            {
                if (!InMemory(p, 4))
                    FillBuffer(p);

                Array.ConstrainedCopy(buffer, (int)(p - bufferOffset), res, 0, 4);
                position++;
            }

            return BitConverter.ToUInt32(res, 0);
        }

        public DateTime ReadDateTime()
        {
            return ReadDateTime(position);
        }

        public DateTime ReadDateTime(long p)
        {
            ushort date = ReadUInt16(p + 0);
            ushort time = ReadUInt16(p + 2);
            return new DateTime(
                date / 0x200 + 1980,
                date % 0x200 / 0x20,
                date % 0x200 % 0x20,
                time / 0x800,
                time % 0x800 / 0x20,
                time % 0x800 % 0x20 * 2);
        }

        public void Seek(long p)
        {
            position = p;
        }

        private bool ValidPosition(long p)
        {
            return p >= 0 && p < Length;
        }

        private bool InMemory(long p, long l)
        {
            return p >= bufferOffset && p + l <= bufferOffset + 0x8000;
        }

        private void FillBuffer(long p)
        {
            bufferOffset = p - 0x4000;
            if (bufferOffset < 0)
                bufferOffset = 0;

            if (bufferOffset + 0x8000 > Length)
                bufferOffset = Length - 0x8000;

            // filesize < 0x8000
            if (bufferOffset < 0)
            {
                bufferOffset = 0;
                stream.Seek(bufferOffset, SeekOrigin.Begin);
                stream.Read(buffer, 0, (int)Length);
            }
            else
            {
                stream.Seek(bufferOffset, SeekOrigin.Begin);
                stream.Read(buffer, 0, 0x8000);
            }

            position = p;
        }
    
        #endregion
    }
}
