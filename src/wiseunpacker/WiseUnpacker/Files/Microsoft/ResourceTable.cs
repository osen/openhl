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
    internal class ResourceTable
    {
        public ushort rscAlignShift { get; private set; }
        public TYPEINFO TypeInfo { get; private set; }
        public ushort rscEndTypes { get; private set; }
        public sbyte[][] rscResourceNames { get; private set; }
        public byte rscEndNames { get; private set; }

        public static ResourceTable Deserialize(MultiPartFile file)
        {
            ResourceTable rt = new ResourceTable();

            rt.rscAlignShift = file.ReadUInt16();
            rt.TypeInfo = TYPEINFO.Deserialize(file);
            rt.rscEndTypes = file.ReadUInt16();
            rt.rscResourceNames = null; // TODO: Figure out size
            rt.rscEndNames = file.ReadByte();

            return rt;
        }
    }
}