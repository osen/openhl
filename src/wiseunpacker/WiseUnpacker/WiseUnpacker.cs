using System;
using System.IO;
using System.Text;
using WiseUnpacker.Files;
using WiseUnpacker.Files.Microsoft;
using WiseUnpacker.Inflation;

namespace WiseUnpacker
{
    public class WiseUnpacker
    {
        // Inflation helper
        private InflateImpl inflate;

        // IO values
        private MultiPartFile inputFile;
        private bool pkzip;

        // Deterministic values
        private static readonly FormatProperty[] knownFormats = FormatProperty.GenerateKnownFormats();
        private FormatProperty currentFormat;
        private long dataBase;

        // Heuristic values
        private long offsetApproximate;
        private long offsetReal;
        private long fileStart;
        private long fileEnd;
        private long extracted;

        /// <summary>
        /// Create a new heuristic unpacker
        /// </summary>
        public WiseUnpacker()
        {
            inflate = null;
        }

        /// <summary>
        /// Attempt to extract a Wise installer
        /// </summary>
        /// <param name="file">Possible Wise installer</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        public bool ExtractTo(string file, string outputPath)
        {
            file = Path.GetFullPath(file);
            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(outputPath);
                        
            if (Open(file))
            {
                // Move to data and determine if this is a known format
                JumpToTheData();
                inputFile.Seek(dataBase + currentFormat.ExecutableLength);
                for (int i = 0; i < knownFormats.Length; i++)
                {
                    if (currentFormat.Equals(knownFormats[i]))
                    {
                        currentFormat = knownFormats[i];
                        break;
                    }
                }

                // Fall back on heuristics if we couldn't match
                if (currentFormat.ArchiveEnd == 0)
                {
                    inputFile.Seek(0);
                    Approximate();
                    if (!pkzip)
                    {
                        if (FindReal(outputPath))
                        {
                            ExtractFiles(outputPath);
                            RenameFiles(outputPath);
                            Close();
                            return true;
                        }
                    }
                    else
                    {
                        offsetReal = offsetApproximate;
                        ExtractFiles(outputPath);
                        RenameFiles(outputPath);
                        Close();
                        return true;
                    }

                    Close();
                    return false;
                }

                // Skip over the addditional DLL name, if we expect it
                long dataStart = currentFormat.ExecutableLength;
                if (currentFormat.Dll)
                {
                    byte[] dll = new byte[256];
                    inputFile.Read(dll, 0, 1);
                    dataStart++;

                    if (dll[0] != 0x00)
                    {
                        inputFile.Read(dll, 1, dll[0]);
                        dataStart += dll[0];

                        int dllLength = inputFile.ReadInt32();
                        dataStart += 4;
                    }
                }

                // Check if flags are consistent
                if (!currentFormat.NoCrc)
                {
                    int flags = inputFile.ReadInt32();
                    if ((flags & 0x0100) != 0)
                        return false;
                }

                if (currentFormat.ArchiveEnd > 0)
                {
                    inputFile.Seek(dataBase + dataStart + currentFormat.ArchiveEnd);
                    int archiveEndLoaded = inputFile.ReadInt32();
                    if (archiveEndLoaded != 0)
                        currentFormat.ArchiveEnd = archiveEndLoaded + dataBase;
                }

                inputFile.Seek(dataBase + dataStart + currentFormat.ArchiveStart);

                // Skip over the initialization text, if we expect it
                if (currentFormat.InitText)
                {
                    byte[] waitingBytes = new byte[256];
                    inputFile.Read(waitingBytes, 0, 1);
                    inputFile.Read(waitingBytes, 1, waitingBytes[0]);
                }

                offsetReal = inputFile.Position;
                ExtractFiles(outputPath);
                RenameFiles(outputPath);

                Close();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Open a potentially-multipart file for analysis and extraction
        /// </summary>
        /// <param name="file">Possible wise installer base</param>
        /// <returns>True if the file could be opened, false otherwise</returns>
        private bool Open(string file)
        {
            inputFile = MultiPartFile.Create(file);
            if (inputFile == null)
                return false;

            file = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));

            int fileno = 2;
            while (inputFile.Append($"{file}.w{fileno / 10 + 48}{fileno % 10 + 48}"))
            {
                fileno++;
            }

            return true;
        }

        /// <summary>
        /// Approximate the file offset for the Wise installer
        /// </summary>
        private void Approximate()
        {
            byte[] buf = new byte[0xc200];
            inputFile.Seek(0);
            inputFile.Read(buf, 0, 0xc000);
            offsetApproximate = 0xbffc;

            int tempOffset;
            while ((buf[offsetApproximate] != 0 || buf[offsetApproximate + 1] != 0) && offsetApproximate > 0x20)
            {
                offsetApproximate -= 1;
                if (buf[offsetApproximate] == 0 && buf[offsetApproximate + 1] == 0)
                {
                    int temp = 0;
                    for (tempOffset = 1; tempOffset <= 0x20; tempOffset++)
                    {
                        if (buf[offsetApproximate - tempOffset] == 0)
                            temp++;
                    }

                    if (temp < 0x4)
                        offsetApproximate -= 2;
                }
            }

            offsetApproximate += 2;
            while (buf[offsetApproximate + 3] == 0 && offsetApproximate + 4 < 0xc000)
            {
                offsetApproximate += 4;
            }

            int tempBlock = 0;
            if (buf[offsetApproximate] <= 0x20 && buf[offsetApproximate + 1] > 0 && buf[offsetApproximate + 1] + offsetApproximate + 3 < 0xc000)
            {
                int temp = buf[offsetApproximate + 1] + 0x2;
                for (tempOffset = 0x2; tempOffset < temp; tempOffset++)
                {
                    if (buf[offsetApproximate + tempOffset] >= 0x80)
                        tempBlock++;
                }

                if (tempBlock * 0x100 / temp < 0x10)
                    offsetApproximate += temp;
            }

            tempOffset = 0x2;
            while (tempBlock != 0x4034b50 && tempOffset < 0x80 && offsetApproximate - tempOffset >= 0 && offsetApproximate - tempOffset <= 0xbffc)
            {
                byte[] tempBlockBuf = new byte[4];
                Array.ConstrainedCopy(buf, (int)(offsetApproximate - tempOffset), tempBlockBuf, 0, 4);
                tempBlock = BitConverter.ToInt32(tempBlockBuf, 0);
                tempOffset++;
            }

            if (tempOffset < 0x80)
            {
                pkzip = true;
                tempOffset = 0;
                int tempFlag = 0;
                while (tempFlag != 0x4034b50 && tempOffset < offsetApproximate)
                {
                    byte[] tempFlagBuf = new byte[4];
                    Array.ConstrainedCopy(buf, tempOffset, tempFlagBuf, 0, 4);
                    tempFlag = BitConverter.ToInt32(tempFlagBuf, 0);
                    tempOffset++;
                }

                tempOffset -= 1;
                offsetApproximate = tempOffset;
                if (tempFlag != 0x4034b50)
                    pkzip = false;
            }
            else
            {
                pkzip = false;
            }
        }

        /// <summary>
        /// Jump to the .data section of an executable stream
        /// </summary>
        private void JumpToTheData()
        {
            currentFormat = new FormatProperty();
            currentFormat.ExecutableType = ExecutableType.Unknown;
            dataBase = 0;
            currentFormat.LCode = 0;
            currentFormat.ExecutableLength = 0; // dataStart

            bool searchAgainAtEnd = true;
            do
            {
                searchAgainAtEnd = false;
                dataBase += currentFormat.ExecutableLength;
                currentFormat.ExecutableLength = 0;

                currentFormat.ExecutableType = ExecutableType.Unknown;
                inputFile.Seek(dataBase + currentFormat.ExecutableLength);
                IMAGE_DOS_HEADER exeHdr = IMAGE_DOS_HEADER.Deserialize(inputFile);

                if ((exeHdr.Magic == Constants.IMAGE_NT_SIGNATURE || exeHdr.Magic == Constants.IMAGE_DOS_SIGNATURE)
                    && exeHdr.HeaderParagraphSize >= 4
                    && exeHdr.NewExeHeaderAddr >= 0x40)
                {
                    currentFormat.ExecutableLength = exeHdr.NewExeHeaderAddr;
                    inputFile.Seek(dataBase + currentFormat.ExecutableLength);
                    exeHdr = IMAGE_DOS_HEADER.Deserialize(inputFile);
                }

                switch (exeHdr.Magic)
                {
                    case Constants.IMAGE_OS2_SIGNATURE:
                        currentFormat.ExecutableType = ProcessNe();
                        break;
                    case Constants.IMAGE_OS2_SIGNATURE_LE:
                    case (ushort)Constants.IMAGE_NT_SIGNATURE:
                        currentFormat.ExecutableType = ProcessPe(ref searchAgainAtEnd);
                        break;
                    default:
                        break;
                }
            }
            while (searchAgainAtEnd);
        }

        /// <summary>
        /// Process an NE executable header
        /// </summary>
        private ExecutableType ProcessNe()
        {
            ExecutableType foundExe = ExecutableType.Unknown;

            inputFile.Seek(dataBase + currentFormat.ExecutableLength);
            IMAGE_OS2_HEADER ne = IMAGE_OS2_HEADER.Deserialize(inputFile);
            long o = currentFormat.ExecutableLength;

            inputFile.Seek(dataBase + currentFormat.ExecutableLength + ne.SegmentTableOffset + 0 * 8 /* sizeof(NewSeg) */);
            NewSeg codeSegInfo = NewSeg.Deserialize(inputFile);

            inputFile.Seek(dataBase + currentFormat.ExecutableLength + ne.SegmentTableOffset + 2 * 8 /* sizeof(NewSeg) */);
            NewSeg dataSegInfo = NewSeg.Deserialize(inputFile);

            // Assumption: there are resources and they are at the end ..
            inputFile.Seek(dataBase + currentFormat.ExecutableLength + ne.ResourceTableOffset);
            short rsAlign = inputFile.ReadInt16();

            // ne.ne_cres is 0 so you have to cheat
            while (inputFile.Position + 8 /* sizeof(rsType) */ <= dataBase + currentFormat.ExecutableLength + ne.ResidentNameTableOffset)
            {
                RsrcTypeInfo rsType = RsrcTypeInfo.Deserialize(inputFile);
                for (int z2 = 1; z2 < rsType.rt_nres; z2++)
                {
                    RsrcNameInfo rsName = RsrcNameInfo.Deserialize(inputFile);
                    int a = rsName.Offset << rsAlign + rsName.Length << rsAlign;
                    if (o < a)
                        o = a;
                }

                currentFormat.ExecutableLength = 0;
                foundExe = ExecutableType.NE;
            }

            return foundExe;
        }

        /// <summary>
        /// Process a PE executable header
        /// </summary>
        private ExecutableType ProcessPe(ref bool searchAgainAtEnd)
        {
            inputFile.Seek(dataBase + currentFormat.ExecutableLength + 4);
            IMAGE_FILE_HEADER ifh = IMAGE_FILE_HEADER.Deserialize(inputFile);
            IMAGE_OPTIONAL_HEADER ioh = IMAGE_OPTIONAL_HEADER.Deserialize(inputFile);

            // Read sections until we have the ones we need
            IMAGE_SECTION_HEADER temp = null;
            IMAGE_SECTION_HEADER resource = null;
            for (int i = 0; i < ifh.NumberOfSections; i++)
            {
                IMAGE_SECTION_HEADER sectionHeader = IMAGE_SECTION_HEADER.Deserialize(inputFile);
                string headerName = Encoding.ASCII.GetString(sectionHeader.Name, 0, 8);

                // .text
                if (headerName.StartsWith(".text"))
                {
                    currentFormat.LCode = sectionHeader.VirtualSize;
                }

                // .rdata
                else if (headerName.StartsWith(".rdata"))
                {
                    // No-op
                }

                // .data
                else if (headerName.StartsWith(".data"))
                {
                    currentFormat.LData = sectionHeader.VirtualSize;
                    if ((ifh.Characteristics & (1 << 0)) == 0)
                        temp = sectionHeader;
                }

                // .rsrc
                else if (headerName.StartsWith(".rsrc"))
                {
                    resource = sectionHeader;
                    if ((ifh.Characteristics & (1 << 0)) != 0)
                        temp = sectionHeader;
                }
            }

            // the unpacker of the self-extractor does not use any resource functions either.
            if (temp.SizeOfRawData > 20000)
            {
                for (int f = 0; f <= 20000 - 0x80; f++)
                {
                    inputFile.Seek(dataBase + temp.PointerToRawData + f);
                    IMAGE_DOS_HEADER exeHdr = IMAGE_DOS_HEADER.Deserialize(inputFile);

                    if ((exeHdr.Magic == Constants.IMAGE_NT_SIGNATURE || exeHdr.Magic == Constants.IMAGE_DOS_SIGNATURE)
                        && exeHdr.HeaderParagraphSize >= 4
                        && exeHdr.NewExeHeaderAddr >= 0x40
                        && (exeHdr.Relocations == 0 || exeHdr.Relocations == 3))
                    {
                        currentFormat.ExecutableLength = (int)temp.PointerToRawData + f;
                        fileEnd = (int)(dataBase + temp.PointerToRawData + ioh.DataDirectory[2].Size);
                        searchAgainAtEnd = true;
                        break;
                    }
                }
            }

            currentFormat.ExecutableLength = (int)(resource.PointerToRawData + resource.SizeOfRawData);
            return ExecutableType.PE;
        }

        /// <summary>
        /// Based on the approximate values, find the real file offset
        /// </summary>
        private bool FindReal(string outputPath)
        {
            if (!pkzip)
            {
                if (offsetApproximate < 0x100)
                    offsetApproximate = 0x100;
                else if (offsetApproximate > 0xbf00)
                    offsetApproximate = 0xbf00;
            }

            int pos;
            uint newcrc = 0;
            if (offsetApproximate >= 0 && offsetApproximate <= 0xffff)
            {
                pos = 0;
                do
                {
                    inputFile.Seek(offsetApproximate + pos);
                    Inflate(inputFile, Path.Combine(outputPath, "WISE0001"));
                    newcrc = inputFile.ReadUInt32();
                    offsetReal = (uint)(offsetApproximate + pos);
                    pos++;
                }
                while ((inflate.CRC != newcrc || inflate.Result != 0 || newcrc == 0) && pos != 0x100);

                if ((inflate.CRC != newcrc || newcrc == 0 || inflate.Result != 0) && pos == 0x100)
                {
                    pos = -1;
                    do
                    {
                        inputFile.Seek(offsetApproximate + pos);
                        Inflate(inputFile, Path.Combine(outputPath, "WISE0001"));
                        newcrc = inputFile.ReadUInt32();
                        offsetReal = (uint)(offsetApproximate + pos);
                        pos -= 1;
                    }
                    while ((inflate.CRC != newcrc || inflate.Result != 0 || newcrc == 0) && pos != -0x100);
                }
            }
            else
            {
                inflate.CRC = ~newcrc;
                pos = -0x100;
            }

            if ((inflate.CRC != newcrc || newcrc == 0 || inflate.Result != 0) && pos == -0x100)
            {
                offsetReal = 0xffffffff;
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Extract all files to the output directory
        /// </summary>
        /// <param name="outputPath">Output directory for extracted files</param>
        private void ExtractFiles(string outputPath)
        {
            uint newcrc = 0;

            Stream dumpFile = File.OpenWrite(Path.Combine(outputPath, "WISE0000"));
            inputFile.Seek((int)offsetReal);
            do
            {
                extracted++;
                long fs = inputFile.Position;
                if (pkzip)
                {
                    byte[] buf = new byte[0x400];
                    inputFile.Read(buf, 0, 0xe);
                    newcrc = inputFile.ReadUInt32();
                    inputFile.Read(buf, 0, 0x8);
                    inputFile.Read(buf, 0, 0x2);
                    short len1 = BitConverter.ToInt16(buf, 0);
                    inputFile.Read(buf, 0, 0x2);
                    short len2 = BitConverter.ToInt16(buf, 0);
                    if (len1 + len2 > 0)
                        inputFile.Read(buf, 0, len1 + len2);
                }

                if (inputFile.Position == inputFile.Length)
                    break;

                Inflate(inputFile, Path.Combine(outputPath, $"WISE{extracted.ToString("X").PadLeft(4, '0')}"));
                fileStart = fs;
                if (pkzip)
                    inflate.CRC = 0x4034b50;

                fileEnd = fileStart + inflate.InputSize - 1;
                if (inflate.Result == 0)
                {
                    newcrc = inputFile.ReadUInt32();
                    int attempt = 0;
                    while (inflate.CRC != newcrc && attempt < 8 && inputFile.Position + 1 < inputFile.Length)
                    {
                        inputFile.Seek(inputFile.Position - 3);
                        newcrc = inputFile.ReadUInt32();
                        attempt++;
                    }

                    fileEnd = inputFile.Position - 1;
                    if (pkzip)
                    {
                        fileEnd -= 4;
                        inputFile.Seek(inputFile.Position - 4);
                    }
                }

                if (inflate.Result != 0 || newcrc != inflate.CRC)
                {
                    inflate.CRC = 0xffffffff;
                    newcrc = 0xfffffffe;
                }

                dumpFile.Write(BitConverter.GetBytes((int)fileStart), 0, 4);
            }
            while (newcrc == inflate.CRC);

            dumpFile.Write(BitConverter.GetBytes((int)fileEnd), 0, 4);
            dumpFile.Close();
        }

        /// <summary>
        /// Run inflation on an input file with an output path
        /// </summary>
        /// <param name="inf">Input multipart file</param>
        /// <param name="outputPath">Output directory for extracted files</param>
        private void Inflate(MultiPartFile inf, string outputPath)
        {
            inflate = new InflateImpl(inf, outputPath);
            inflate.SI_INFLATE();
            inflate.Close();
        }

        /// <summary>
        /// Rename extracted files based on Wise installer names
        /// </summary>
        /// <param name="outputPath">Output directory for extracted files</param>
        private void RenameFiles(string outputPath)
        {
            ReadOnlyFile extractedFile = null;
            string newName = string.Empty;
            long l;
            int sh0, sh1, l1, l2, l3 = 0, l4, l5;

            int res = 1;
            int instcnt = 0;
            int fileno = 1;
            for (; fileno < extracted && fileno < 6 && res != 0; fileno++)
            {
                extractedFile = new ReadOnlyFile(outputPath, $"WISE{fileno.ToString("X").PadLeft(4, '0')}");
                l = 0;
                while (res != 0 && l < extractedFile.Length)
                {
                    while (l < extractedFile.Length && (extractedFile.ReadByte(l + 0) != 0x25 || extractedFile.ReadByte(l + 1) != 0x5c))
                    {
                        l++;
                    }

                    if (l < extractedFile.Length)
                    {
                        l1 = 1;
                        while (l1 < 0x40 && (extractedFile.ReadByte(l - l1 + 0) != 0x25 || extractedFile.ReadByte(l - l1 + 1) == 0x5c))
                        {
                            l1++;
                        }

                        if (l1 < 0x40)
                            res = 0;
                        else
                            l++;
                    }
                }

                if (res != 0)
                    extractedFile.Close();
            }

            if (fileno < 6 && fileno < extracted)
            {
                ReadOnlyFile df = new ReadOnlyFile(outputPath, "WISE0000");
                l5 = (int)((df.Length - 0x4) / 0x4);

                do
                {
                    do
                    {
                        l1 = df.ReadInt32(l5 * 0x4 - 0x4);
                        l2 = df.ReadInt32(l5 * 0x4 - 0);
                        l = extractedFile.Length - 0x7;
                        res = 1;

                        while (l >= 0 && res != 0)
                        {
                            l -= 1;
                            l3 = extractedFile.ReadInt32(l + 0);
                            l4 = extractedFile.ReadInt32(l + 0x4);
                            if (l4 > l3 && l4 < l2 && l3 < l1 && l4 - l3 == l2 - l1)
                                res = 0;
                        }

                        if (res != 0)
                            l5 -= 1;
                    }
                    while (res != 0 && l5 != 0);

                    sh0 = l1 - l3;

                    if (res == 0)
                    {
                        do
                        {
                            l1 = df.ReadInt32(l5 * 0x4 - 0x4);
                            l2 = df.ReadInt32(l5 * 0x4 - 0);
                            l = extractedFile.Length - 0x7;
                            res = 1;
                            while (l >= 0 && res != 0)
                            {
                                l -= 1;
                                l3 = extractedFile.ReadInt32(l + 0);
                                l4 = extractedFile.ReadInt32(l + 0x4);
                                if (l4 > l3 && l4 < l2 && l3 < l1 && l4 - l3 == l2 - l1)
                                    res = 0;
                            }

                            if (res != 0)
                                l5 -= 1;
                        }
                        while (res != 0 && l5 != 0);
                    }

                    sh1 = l1 - l3;
                }
                while (l5 != 0 && (res != 0 || sh0 != sh1));

                if (res == 0)
                {
                    /* shiftvalue = sh0 */
                    l5 = -0x4;
                    while (l5 + 8 < df.Length)
                    {
                        l5 += 0x4;
                        l1 = df.ReadInt32(l5 + 0);
                        l2 = df.ReadInt32(l5 + 0x4);
                        uint l0 = 0xffffffff;
                        res = 1;
                        while (l0 + 0x29 < extractedFile.Length && res != 0)
                        {
                            l0 += 1;
                            l3 = extractedFile.ReadInt32(l0 + 0);
                            l4 = extractedFile.ReadInt32(l0 + 0x4);
                            if (l1 == l3 + sh0 && l2 == l4 + sh0)
                                res = 0;
                        }

                        int offset = 0;
                        if (res == 0)
                        {
                            l2 = extractedFile.ReadInt16(l0 - 2);
                            newName = "";
                            offset = (int)l0;
                            l0 += 0x28;
                            res = 2;
                            if (extractedFile.ReadByte(l0) == 0x25)
                            {
                                while (extractedFile.ReadByte(l0) != 0)
                                {
                                    newName = newName + (char)extractedFile.ReadByte(l0);
                                    if (extractedFile.ReadByte(l0) < 0x20)
                                        res = 1;

                                    if (extractedFile.ReadByte(l0) == 0x25 && res != 1)
                                        res = 3;

                                    if (extractedFile.ReadByte(l0) == 0x5c && extractedFile.ReadByte(l0 - 1) == 0x25 && res == 3)
                                        res = 4;

                                    if (res == 4)
                                        res = 0;

                                    l0++;
                                }
                            }

                            if (res != 0)
                                res = 0x80;
                        }

                        l1 = (l5 + 0x4) / 0x4;
                        if (res == 0)
                        {
                            newName = newName.Replace("%", string.Empty).Replace("\\\\", "\\");
                            newName = Path.Combine(outputPath, newName);

                            string fname = Path.Combine(outputPath, $"WISE{l1.ToString("X").PadLeft(4, '0')}");

                            /* Make directories */
                            string nnDir = Path.GetDirectoryName(newName);
                            Directory.CreateDirectory(nnDir);

                            /* Rename file */
                            File.Delete(newName);
                            File.Move(fname, newName);
                            var dt = extractedFile.ReadDateTime(offset + 0x8);
                            File.SetCreationTime(newName, dt);
                            File.SetLastWriteTime(newName, dt);
                        }
                        else if (res == 0x80)
                        {
                            instcnt++;

                            /* Rename file */
                            File.Delete(Path.Combine(outputPath, $"INST{instcnt.ToString("X").PadLeft(4, '0')}"));
                            File.Move(Path.Combine(outputPath, $"WISE{l1.ToString("X").PadLeft(4, '0')}"),
                                Path.Combine(outputPath, $"INST{instcnt.ToString("X").PadLeft(4, '0')}"));
                        }
                    }
                }

                df.Close();
                extractedFile.Close();
            }
        }

        /// <summary>
        /// Close the possible Wise installer
        /// </summary>
        private void Close()
        {
            inputFile.Close();
        }
    }
}
