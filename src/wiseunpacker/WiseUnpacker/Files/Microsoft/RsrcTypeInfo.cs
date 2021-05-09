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
    /// <summary>
    /// Resource type information block
    /// </summary>
    internal class RsrcTypeInfo
    {
        public ushort ID { get; private set; }
        public ushort rt_nres { get; private set; }
        public uint rt_proc { get; private set; }

        public static RsrcTypeInfo Deserialize(MultiPartFile file)
        {
            RsrcTypeInfo rti = new RsrcTypeInfo();

            rti.ID = file.ReadUInt16();
            rti.rt_nres = file.ReadUInt16();
            rti.rt_proc = file.ReadUInt32();

            return rti;
        }
    }
}