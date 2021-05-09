/*
 *	  NEWEXE.H (C) Copyright Microsoft Corp 1984-1987
 *
 *	  Data structure definitions for the OS/2 & Windows
 *	  executable file format.
 *
 *	  Modified by IVS on 24-Jan-1991 for Resource DeCompiler
 *	  (C) Copyright IVS 1991
 *
 *    http://csn.ul.ie/~caolan/pub/winresdump/winresdump/newexe.h
 */

namespace WiseUnpacker.Files.Microsoft
{
    internal class IMAGE_OPTIONAL_HEADER
    {
        // Standard fields

        public ushort Magic { get; private set; }
        public byte MajorLinkerVersion { get; private set; }
        public byte MinorLinkerVersion { get; private set; }
        public uint SizeOfCode { get; private set; }
        public uint SizeOfInitializedData { get; private set; }
        public uint SizeOfUninitializedData { get; private set; }
        public uint AddressOfEntryPoint { get; private set; }
        public uint BaseOfCode { get; private set; }
        public uint BaseOfData { get; private set; }

        // NT additional fields.

        public uint ImageBase { get; private set; }
        public uint SectionAlignment { get; private set; }
        public uint FileAlignment { get; private set; }
        public ushort MajorOperatingSystemVersion { get; private set; }
        public ushort MinorOperatingSystemVersion { get; private set; }
        public ushort MajorImageVersion { get; private set; }
        public ushort MinorImageVersion { get; private set; }
        public ushort MajorSubsystemVersion { get; private set; }
        public ushort MinorSubsystemVersion { get; private set; }
        public uint Reserved1 { get; private set; }
        public uint SizeOfImage { get; private set; }
        public uint SizeOfHeaders { get; private set; }
        public uint CheckSum { get; private set; }
        public ushort Subsystem { get; private set; }
        public ushort DllCharacteristics { get; private set; }
        public uint SizeOfStackReserve { get; private set; }
        public uint SizeOfStackCommit { get; private set; }
        public uint SizeOfHeapReserve { get; private set; }
        public uint SizeOfHeapCommit { get; private set; }
        public uint LoaderFlags { get; private set; }
        public uint NumberOfRvaAndSizes { get; private set; }
        public IMAGE_DATA_DIRECTORY[] DataDirectory { get; private set; }

        public static IMAGE_OPTIONAL_HEADER Deserialize(MultiPartFile file)
        {
            IMAGE_OPTIONAL_HEADER ioh = new IMAGE_OPTIONAL_HEADER();

            ioh.Magic = file.ReadUInt16();
            ioh.MajorLinkerVersion = file.ReadByte();
            ioh.MinorLinkerVersion = file.ReadByte();
            ioh.SizeOfCode = file.ReadUInt32();
            ioh.SizeOfInitializedData = file.ReadUInt32();
            ioh.SizeOfUninitializedData = file.ReadUInt32();
            ioh.AddressOfEntryPoint = file.ReadUInt32();
            ioh.BaseOfCode = file.ReadUInt32();
            ioh.BaseOfData = file.ReadUInt32();

            ioh.ImageBase = file.ReadUInt32();
            ioh.SectionAlignment = file.ReadUInt32();
            ioh.FileAlignment = file.ReadUInt32();
            ioh.MajorOperatingSystemVersion = file.ReadUInt16();
            ioh.MinorOperatingSystemVersion = file.ReadUInt16();
            ioh.MajorImageVersion = file.ReadUInt16();
            ioh.MinorImageVersion = file.ReadUInt16();
            ioh.MajorSubsystemVersion = file.ReadUInt16();
            ioh.MinorSubsystemVersion = file.ReadUInt16();
            ioh.Reserved1 = file.ReadUInt32();
            ioh.SizeOfImage = file.ReadUInt32();
            ioh.SizeOfHeaders = file.ReadUInt32();
            ioh.CheckSum = file.ReadUInt32();
            ioh.Subsystem = file.ReadUInt16();
            ioh.DllCharacteristics = file.ReadUInt16();
            ioh.SizeOfStackReserve = file.ReadUInt32();
            ioh.SizeOfStackCommit = file.ReadUInt32();
            ioh.SizeOfHeapReserve = file.ReadUInt32();
            ioh.SizeOfHeapCommit = file.ReadUInt32();
            ioh.LoaderFlags = file.ReadUInt32();
            ioh.NumberOfRvaAndSizes = file.ReadUInt32();
            ioh.DataDirectory = new IMAGE_DATA_DIRECTORY[Constants.IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
            for (int i = 0; i < Constants.IMAGE_NUMBEROF_DIRECTORY_ENTRIES; i++)
            {
                ioh.DataDirectory[i] = IMAGE_DATA_DIRECTORY.Deserialize(file);
            }

            return ioh;
        }
    }
}