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
    /// DOS 1, 2, 3 .EXE header
    /// </summary>
    internal class IMAGE_DOS_HEADER
    {
        public ushort Magic { get; private set; }                   // 00 Magic number
        public ushort LastPageBytes { get; private set; }           // 02 Bytes on last page of file
        public ushort Pages { get; private set; }                   // 04 Pages in file
        public ushort Relocations { get; private set; }             // 06 Relocations
        public ushort HeaderParagraphSize { get; private set; }     // 08 Size of header in paragraphs
        public ushort MinimumExtraParagraphs { get; private set; }  // 0A Minimum extra paragraphs needed
        public ushort MaximumExtraParagraphs { get; private set; }  // 0C Maximum extra paragraphs needed
        public ushort InitialSSValue { get; private set; }          // 0E Initial (relative) SS value
        public ushort InitialSPValue { get; private set; }          // 10 Initial SP value
        public ushort Checksum { get; private set; }                // 12 Checksum
        public ushort InitialIPValue { get; private set; }          // 14 Initial IP value
        public ushort InitialCSValue { get; private set; }          // 16 Initial (relative) CS value
        public ushort RelocationTableAddr { get; private set; }     // 18 File address of relocation table
        public ushort OverlayNumber { get; private set; }           // 1A Overlay number
        public ushort[] Reserved1 { get; private set; }             // 1C Reserved words
        public ushort OEMIdentifier { get; private set; }           // 24 OEM identifier (for e_oeminfo)
        public ushort OEMInformation { get; private set; }          // 26 OEM information; e_oemid specific
        public ushort[] Reserved2 { get; private set; }             // 28 Reserved words
        public int NewExeHeaderAddr { get; private set; }           // 3C File address of new exe header

        public static IMAGE_DOS_HEADER Deserialize(MultiPartFile file)
        {
            IMAGE_DOS_HEADER idh = new IMAGE_DOS_HEADER();

            idh.Magic = file.ReadUInt16();
            idh.LastPageBytes = file.ReadUInt16();
            idh.Pages = file.ReadUInt16();
            idh.Relocations = file.ReadUInt16();
            idh.HeaderParagraphSize = file.ReadUInt16();
            idh.MinimumExtraParagraphs = file.ReadUInt16();
            idh.MaximumExtraParagraphs = file.ReadUInt16();
            idh.InitialSSValue = file.ReadUInt16();
            idh.InitialSPValue = file.ReadUInt16();
            idh.Checksum = file.ReadUInt16();
            idh.InitialIPValue = file.ReadUInt16();
            idh.InitialCSValue = file.ReadUInt16();
            idh.RelocationTableAddr = file.ReadUInt16();
            idh.OverlayNumber = file.ReadUInt16();
            idh.Reserved1 = new ushort[Constants.ERES1WDS];
            for (int i = 0; i < Constants.ERES1WDS; i++)
            {
                idh.Reserved1[i] = file.ReadUInt16();
            }
            idh.OEMIdentifier = file.ReadUInt16();
            idh.OEMInformation = file.ReadUInt16();
            idh.Reserved2 = new ushort[Constants.ERES2WDS];
            for (int i = 0; i < Constants.ERES2WDS; i++)
            {
                idh.Reserved2[i] = file.ReadUInt16();
            }
            idh.NewExeHeaderAddr = file.ReadInt32();

            return idh;
        }
    }
}
