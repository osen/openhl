using System;
using System.IO;
using System.Text;

namespace WiseUnpacker.Files
{
    internal class MultiPartFile
    {
        private readonly Stream stream;
        private long partStart;
        private long partEnd;
        private MultiPartFile next;

        public long Position { get; private set; }
        public long Length { get; private set; }

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        private MultiPartFile(string name) : this(File.OpenRead(name)) { }

        /// <summary>
        /// Constructor
        /// </summary>
        private MultiPartFile(Stream stream)
        {
            this.stream = stream;
            this.partStart = 0;
            this.Position = 0;
            this.partEnd = stream.Length - 1;
            this.Length = partEnd + 1;
            this.next = null;
        }

        /// <summary>
        /// Safely create a new MultiPartFile from a file path
        /// </summary>
        public static MultiPartFile Create(string name)
        {
            if (!File.Exists(name))
                return null;
            
            return new MultiPartFile(name);
        }

        /// <summary>
        /// Safely create a new MultiPartFile from a stream
        /// </summary>
        public static MultiPartFile Create(Stream stream)
        {
            if (stream == null || !stream.CanRead)
                return null;
            
            return new MultiPartFile(stream);
        }

        #endregion

        #region Appending

        /// <summary>
        /// Append a new MultiPartFile to the current one
        /// </summary>
        /// <param name="file">New file path to append</param>
        public bool Append(string file) => Append(Create(file));

        /// <summary>
        /// Append a new MultiPartFile to the current one
        /// </summary>
        /// <param name="stream">Stream to append</param>
        public bool Append(Stream stream) => Append(Create(stream));

        /// <summary>
        /// Append a new MultiPartFile to the current one
        /// </summary>
        /// <param name="mpf">New MultiPartFile to append</param>
        private bool Append(MultiPartFile mpf)
        {
            // If the part is invalid, we can't append
            if (mpf == null)
                return false;

            // Find the current last part
            var mf = this;
            while (next != null)
            {
                mf = mf.next;
            }

            // Assign the new part as the new end
            mf.next = mpf;
            mf.next.partStart = this.Length;
            mf.next.partEnd += this.Length;
            this.Length = mf.next.partEnd + 1;
            
            return true;
        }

        #endregion

        #region Stream Wrappers

        /// <summary>
        /// Seek to a position in the MultiPartFile
        /// </summary>
        public void Seek(long pos)
        {
            Position = pos;
        }

        /// <summary>
        /// Close all parts of this MultiPartFile
        /// </summary>
        public void Close()
        {
            if (next != null)
                next.Close();

            stream.Close();
        }

        /// <summary>
        /// Read from the MultiPartFile to a buffer
        /// </summary>
        public bool Read(byte[] x, int offset, int amount)
        {
            int bufpos;

            MultiPartFile mf = this;
            while (Position > mf.partEnd && mf.next != null)
                mf = mf.next;

            if (Position <= mf.partEnd)
            {
                mf.stream.Seek(Position - mf.partStart, SeekOrigin.Begin);
                if (mf.partEnd + 1 - Position >= amount)
                {
                    mf.stream.Read(x, offset, amount);
                    Position += amount;
                }
                else
                {
                    byte[] buf = new byte[0xffff];
                    bufpos = 0;
                    do
                    {
                        if (mf.partEnd + 1 < Position + amount - bufpos)
                        {
                            mf.stream.Read(buf, bufpos, (int)(mf.partEnd + 1 - Position));
                            bufpos += (int)(mf.partEnd + 1 - Position);
                            Position = mf.partEnd + 1;
                            mf = mf.next;
                        }
                        else
                        {
                            mf.stream.Read(buf, bufpos, amount - bufpos);
                            Position += amount - bufpos;
                            bufpos = amount;
                        }
                    }
                    while (bufpos != amount);

                    Array.ConstrainedCopy(buf, 0, x, offset, amount);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Read a byte from the MultiPartFile
        /// </summary>
        public byte ReadByte()
        {
            byte[] x = new byte[1];
            Read(x, 0, 1);
            return x[0];
        }

        /// <summary>
        /// Read a byte array from the MultiPartFile
        /// </summary>
        public byte[] ReadBytes(int count)
        {
            byte[] x = new byte[count];
            Read(x, 0, count);
            return x;
        }

        /// <summary>
        /// Read a character from the MultiPartFile
        /// </summary>
        public char ReadChar()
        {
            byte[] x = new byte[1];
            Read(x, 0, 1);
            return (char)x[0];
        }

        /// <summary>
        /// Read a character array from the MultiPartFile
        /// </summary>
        public char[] ReadChars(int count)
        {
            byte[] x = new byte[count];
            Read(x, 0, count);
            return Encoding.Default.GetString(x).ToCharArray();
        }

        /// <summary>
        /// Read a short from the MultiPartFile
        /// </summary>
        public short ReadInt16()
        {
            byte[] x = new byte[2];
            Read(x, 0, 2);
            return BitConverter.ToInt16(x, 0);
        }

        /// <summary>
        /// Read a ushort from the MultiPartFile
        /// </summary>
        public ushort ReadUInt16()
        {
            byte[] x = new byte[2];
            Read(x, 0, 2);
            return BitConverter.ToUInt16(x, 0);
        }

        /// <summary>
        /// Read an int from the MultiPartFile
        /// </summary>
        public int ReadInt32()
        {
            byte[] x = new byte[4];
            Read(x, 0, 4);
            return BitConverter.ToInt32(x, 0);
        }

        /// <summary>
        /// Read a uint from the MultiPartFile
        /// </summary>
        public uint ReadUInt32()
        {
            byte[] x = new byte[4];
            Read(x, 0, 4);
            return BitConverter.ToUInt32(x, 0);
        }

        /// <summary>
        /// Read a long from the MultiPartFile
        /// </summary>
        public long ReadInt64()
        {
            byte[] x = new byte[8];
            Read(x, 0, 8);
            return BitConverter.ToInt64(x, 0);
        }

        /// <summary>
        /// Read a ulong from the MultiPartFile
        /// </summary>
        public ulong ReadUInt64()
        {
            byte[] x = new byte[8];
            Read(x, 0, 8);
            return BitConverter.ToUInt64(x, 0);
        }
    
        #endregion
    }
}
