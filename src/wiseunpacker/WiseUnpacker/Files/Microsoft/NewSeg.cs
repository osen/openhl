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
    /// New .EXE segment table entry
    /// </summary>
    internal class NewSeg
    {
        /// <summary>
        /// File sector of start of segment
        /// </summary>
        public ushort StartFileSector { get; private set; }
        
        /// <summary>
        /// Number of bytes in file
        /// </summary>
        public ushort BytesInFile { get; private set; }
        
        /// <summary>
        /// Attribute flags
        /// </summary>
        public ushort Flags { get; private set; }
        
        /// <summary>
        /// Minimum allocation in bytes
        /// </summary>
        public ushort MinimumAllocation { get; private set; }

        public static NewSeg Deserialize(MultiPartFile file)
        {
            NewSeg ns = new NewSeg();

            ns.StartFileSector = file.ReadUInt16();
            ns.BytesInFile = file.ReadUInt16();
            ns.Flags = file.ReadUInt16();
            ns.MinimumAllocation = file.ReadUInt16();

            return ns;
        }
    }
}
