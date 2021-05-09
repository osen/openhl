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
    internal class NAMEINFO
    {
        public ushort Offset { get; private set; }
        public ushort Length { get; private set; }
        public ushort Flags { get; private set; }
        public ushort ID { get; private set; }
        public ushort Handle { get; private set; }
        public ushort Usage { get; private set; }

        public static NAMEINFO Deserialize(MultiPartFile file)
        {
            NAMEINFO ni = new NAMEINFO();

            ni.Offset = file.ReadUInt16();
            ni.Length = file.ReadUInt16();
            ni.Flags = file.ReadUInt16();
            ni.ID = file.ReadUInt16();
            ni.Handle = file.ReadUInt16();
            ni.Usage = file.ReadUInt16();

            return ni;
        }
    }
}