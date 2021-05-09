using System;
using System.IO;
using WiseUnpacker.Files;

namespace WiseUnpacker.Inflation
{
    internal class InflateImpl : Inflate
    {
        private MultiPartFile inputFile;
        private Stream outputFile;
        private byte[] inputBuffer;
        private int inputBufferPosition;
        private int inputBufferSize;
        private CRC32 crc;

        public long InputSize { get; private set; }
        public long OutputSize { get; private set; }
        public int Result { get; private set; }
        public uint CRC
        {
            get
            {
                return crc.Value;
            }
            set
            {
                crc.Value = value;
            }
        }

        public InflateImpl(MultiPartFile inf, string outf)
        {
            inputBuffer = new byte[0x4000];
            inputFile = inf;
            outputFile = File.OpenWrite(outf);
            InputSize = 0;
            OutputSize = 0;
            inputBufferSize = inputBuffer.Length;
            inputBufferPosition = inputBufferSize;
            crc = new CRC32();
        }

        public override void SI_WRITE(int w)
        {
            OutputSize += w;
            outputFile.Write(SI_WINDOW, 0, w);
            crc.Update(SI_WINDOW, 0, w);
        }

        public override byte SI_READ()
        {
            if (inputBufferPosition >= inputBufferSize)
            {
                if (inputFile.Position == inputFile.Length)
                {
                    SI_BREAK = true;
                    return Byte.MaxValue;
                }
                else
                {
                    if (inputBufferSize > inputFile.Length - inputFile.Position)
                        inputBufferSize = (int)(inputFile.Length - inputFile.Position);

                    inputFile.Read(inputBuffer, 0, inputBufferSize);
                    inputBufferPosition = 0;
                }
            }

            byte result = inputBuffer[inputBufferPosition];
            InputSize++;
            inputBufferPosition++;
            return result;
        }

        public void Close()
        {
            inputFile.Seek(inputFile.Position - inputBufferSize + inputBufferPosition);

            crc.Finalize();
            Result = SI_ERROR;

            outputFile.Close();
            inputBuffer = null;
        }
    }
}
