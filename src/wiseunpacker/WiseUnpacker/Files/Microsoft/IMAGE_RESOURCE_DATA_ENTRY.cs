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
    internal class IMAGE_RESOURCE_DATA_ENTRY
    {
        public uint OffsetToData { get; private set; }
        public uint Size { get; private set; }
        public uint CodePage { get; private set; }
        public uint Reserved { get; private set; }

        public static IMAGE_RESOURCE_DATA_ENTRY Deserialize(MultiPartFile file)
        {
            IMAGE_RESOURCE_DATA_ENTRY irde = new IMAGE_RESOURCE_DATA_ENTRY();

            irde.OffsetToData = file.ReadUInt32();
            irde.Size = file.ReadUInt32();
            irde.CodePage = file.ReadUInt32();
            irde.Reserved = file.ReadUInt32();

            return irde;
        }
    }
}