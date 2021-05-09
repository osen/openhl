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
    internal class TYPEINFO
    {
        public ushort TypeID { get; private set; }
        public ushort ResourceCount { get; private set; }
        public uint Reserved { get; private set; }
        public NAMEINFO NameInfo { get; private set; }

        public static TYPEINFO Deserialize(MultiPartFile file)
        {
            TYPEINFO ti = new TYPEINFO();

            ti.TypeID = file.ReadUInt16();
            ti.ResourceCount = file.ReadUInt16();
            ti.Reserved = file.ReadUInt32();
            ti.NameInfo = NAMEINFO.Deserialize(file);

            return ti;
        }
    }
}