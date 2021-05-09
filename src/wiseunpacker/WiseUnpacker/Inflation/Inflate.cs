using System;

namespace WiseUnpacker.Inflation
{
    public abstract class Inflate
    {
        // window size--must be a power of two, and at least 32K for zip's deflate 
        public const int WSIZE = 0x8000;

        public byte[] SI_WINDOW = new byte[WSIZE];
        public int SI_POSITION;

        #region Abstract methods

        public abstract byte SI_READ();
        public abstract void SI_WRITE(int w);

        #endregion

        /// <summary>
        /// The break variable, set it to TRUE if you want to break the
        /// inflate process at any time (use it in SI_READ or SI_WRITE)
        /// </summary>
        protected static bool SI_BREAK = false;

        /// <summary>
        /// This is the error indicator which is set when breaking an inflate
        /// process or when an error occured. The following value table let's you
        /// conclude what have been going wrong:
        /// SI_ERROR (word bitform hi-lo)  ee--|-----|--at|hhbb
        ///     e = error occured          0   = no error
        ///                                1   = error during inflate
        ///                                2   = user break during inflate
        ///     a = block header error     0   = no error
        ///                                1   = header corrupt (only types 0/2)
        ///                                      on block type 0 this means
        ///                                      that the checksum is wrong,
        ///                                      on block type 2 this means
        ///                                      one of the huffman tables
        ///                                      it wrong, check t and h
        ///     t = huffman error type     0   = incomplete
        ///                                1   = full
        ///     h = huffman error tree     0   = no error
        ///                                1   = codelength tree (error)
        ///                                2   = literal/lengthcode tree (error)
        ///                                3   = distance tree (error)
        ///     b = actual block type      0-2 = block type b
        ///                                3   = illegal block type (error)
        /// </summary>
        protected static int SI_ERROR = 0;

        /* лллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллл */
        /* лл DEFLATE STATIC TABLES block ллллллллллллллллллллллллллллллллллллл */
        /* лллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллл */

        private int[] LengthcodeValueOffset = new int[0x11e];  // [0x101, 0x11d]
        private byte[] LengthcodeValueExtrabits = new byte[0x11e]; // [0x101, 0x11d]
        private int[] DistancecodeValueOffset = new int[0x1e];  // [0x000, 0x01d] 
        private byte[] DistancecodeValueExtrabits = new byte[0x1e]; // [0x000, 0x01d] 

        /// <summary>
        /// Allocates and generates the tables
        /// </summary>
        protected void AllocateStaticTables()
        {
            LengthcodeValueOffset = new int[0x11e];
            LengthcodeValueExtrabits = new byte[0x11e];
            DistancecodeValueOffset = new int[0x1e];
            DistancecodeValueExtrabits = new byte[0x1e];

            byte LengthcodeExtrabits = 0;
            byte DistancecodeExtrabits = 0;
            int LengthcodeOffset = 0x03;
            int DistancecodeOffset = 0x01;

            LengthcodeValueOffset[0x11d] = 0x0102;
            LengthcodeValueExtrabits[0x11d] = 0;

            for (byte pos = 0x00; pos <= 0x1d; pos++)
            {
                // increase number of extra bits for length code table every 4th value
                if (pos >= 0x08 && ((pos & 0x03) == 0))
                    LengthcodeExtrabits++;

                // increase number of extra bits for distance code table every 2nd value
                if (pos >= 0x04 && ((pos & 0x01) == 0))
                    DistancecodeExtrabits++;

                // for pos<=$1c put value entry into length code table
                if (pos <= 0x1b) LengthcodeValueOffset[pos + 0x101] = LengthcodeOffset;
                if (pos <= 0x1b) LengthcodeValueExtrabits[pos + 0x101] = LengthcodeExtrabits;

                // put value entry into distance code table
                DistancecodeValueOffset[pos] = DistancecodeOffset;
                DistancecodeValueExtrabits[pos] = DistancecodeExtrabits;

                // increase length and distance code values
                LengthcodeOffset += (1 << LengthcodeExtrabits);
                DistancecodeOffset += (1 << DistancecodeExtrabits);
            }
        }

        protected void DeAllocateStaticTables()
        {
            LengthcodeValueOffset = null;
            LengthcodeValueExtrabits = null;
            DistancecodeValueOffset = null;
            DistancecodeValueExtrabits = null;
        }

        /* лллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллл */
        /* лл DEFLATE HUFFMAN TREE block  ллллллллллллллллллллллллллллллллллллл */
        /* лллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллл */

        /// <summary>
        /// The huffman-tree node structure, differs from common specifications
        /// </summary>
        protected class HuffmanNode
        {
            /// <summary>
            /// The value of the acutal alphabet entry
            /// </summary>
            public int value;

            /// <summary>
            /// The pointers to the children for both directions,
            /// if null then there's no child for the actual direction
            /// </summary>
            public HuffmanNode[] next = new HuffmanNode[2];

            /// <summary>
            /// Tells us if the nodes in both directions either are
            /// end nodes or not
            /// </summary>
            public bool[] endnode = new bool[2];
        }

        /// <summary>
        /// Creates a virgin huffman tree root
        /// </summary>
        protected HuffmanNode CreateNewHuffmanTree()
        {
            return new HuffmanNode()
            {
                value = 0xffff,
                next = new HuffmanNode[] { null, null },
                endnode = new bool[] { false, false },
            };
        }

        /// <summary>
        /// Adds a child in direction `newdirection` to the tree and gives the child
        /// the value `newvalue`. If `newvalue` is set between 0x0000 and 0xfffe the
        /// child is seen as an endnode, if 0xffff the child is just another node with
        /// two children
        /// </summary>
        protected void AddNewChildToHuffmanNode(ref HuffmanNode HuffmanNode, byte newdirection, int newvalue)
        {
            HuffmanNode newnode = new HuffmanNode()
            {
                value = newvalue,
                next = new HuffmanNode[] { null, null },
                endnode = new bool[] { false, false },
            };

            if (newvalue < 0xffff)
                HuffmanNode.endnode[newdirection] = true;

            HuffmanNode.next[newdirection] = newnode;
        }

        /// <summary>
        /// Tries to add a new codelength to the tree, but beware:
        /// It only returns FALSE if no space is found for a value! This means e.g.
        /// that if you have a complete huffman tree with all codelengths=8 and
        /// you try to add a value with codelength<8 it will return TRUE and writes
        /// the code as the bitstream 0000000 (always left direction).
        /// So you have to fulfill the following rules:
        ///   - always add the values with short codelengths before you add
        ///     the values with codelengths which are longer
        ///   - to test if the tree is complete try to add another value
        ///     with the highest codelength you've used yet for the tree,
        ///     if TRUE is the result, the tree is incomplete,
        ///     if FALSE is the result, the tree is complete
        /// </summary>
        protected bool AddNewCodeToHuffmanTree(HuffmanNode HuffmanNode, byte Codelength, int Codevalue)
        {
            bool result = false;

            // impossible codelength
            if (Codelength == 0)
                return result;

            if (!HuffmanNode.endnode[0])
            {
                if (HuffmanNode.next[0] == null)
                    AddNewChildToHuffmanNode(ref HuffmanNode, 0, 0xffff);

                if (Codelength == 1)
                {
                    HuffmanNode.endnode[0] = true;
                    HuffmanNode.next[0].value = Codevalue;
                    result = true;
                }
                else
                {
                    result = AddNewCodeToHuffmanTree(HuffmanNode.next[0], (byte)(Codelength - 1), Codevalue);
                }
            }
            else
            {
                result = false;
            }

            if (result == false)
            {
                if (!HuffmanNode.endnode[1])
                {
                    HuffmanNode.endnode[0] = true;
                    if (HuffmanNode.next[1] == null)
                        AddNewChildToHuffmanNode(ref HuffmanNode, 1, 0xffff);

                    if (Codelength == 1)
                    {
                        HuffmanNode.endnode[1] = true;
                        HuffmanNode.next[1].value = Codevalue;
                        result = true;
                    }
                    else
                    {
                        result = AddNewCodeToHuffmanTree(HuffmanNode.next[1], (byte)(Codelength - 1), Codevalue);
                    }
                }
                else
                {
                    result = false;
                    HuffmanNode.endnode[1] = true;
                }
            }

            return result;
        }

        protected void FreeHuffmanTree(ref HuffmanNode HuffmanNode)
        {
            if (HuffmanNode.next[0] != null) FreeHuffmanTree(ref HuffmanNode.next[0]);
            if (HuffmanNode.next[1] != null) FreeHuffmanTree(ref HuffmanNode.next[1]);
            HuffmanNode = null;
        }

        /* лллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллл */
        /* лл BITWISE READ block лллллллллллллллллллллллллллллллллллллллллллллл */
        /* лллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллл */

        private byte BitBuffer;
        private byte BitNumber;

        /// <summary>
        /// Reads `NumberOfBits` bits from the input stream and returns it/them
        /// as a longint. This causes a little speed decrease but assures that
        /// not too much bytes are read and the caller doesn't have to calculate
        /// the byte/word/integer/whatever value by hand.
        /// </summary>
        protected int ReadBits(byte NumberOfBits)
        {
            int ResultMask = 1;
            int Result = 0;
            while (NumberOfBits > 0)
            {
                if (BitNumber == 8)
                {
                    BitNumber = 0;
                    BitBuffer = SI_READ();
                }

                Result += (BitBuffer & 0x1) * ResultMask;
                ResultMask <<= 0x1;
                BitBuffer = (byte)((uint)(BitBuffer) >> 0x1);
                BitNumber++;
                NumberOfBits--;
            }

            return Result;
        }

        /// <summary>
        /// Reads one bits from the input stream and returns it/them
        /// as a byte.
        /// </summary>
        protected byte ReadBit()
        {
            if (BitNumber == 8)
            {
                BitNumber = 0;
                BitBuffer = SI_READ();
            }

            byte Result = (byte)(BitBuffer & 0x1);
            BitBuffer = (byte)((uint)(BitBuffer) >> 0x1);
            BitNumber += 1;
            return Result;
        }

        /* лллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллл */
        /* лл HUFFMAN-TREE-USING block лллллллллллллллллллллллллллллллллллллллл */
        /* лллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллл */

        private byte[] CodelengthOrder = new byte[] { 0x10, 0x11, 0x12, 0, 0x8, 0x7, 0x9, 0x6, 0xa, 0x5, 0xb, 0x4, 0xc, 0x3, 0xd, 0x2, 0xe, 0x1, 0xf };
        private byte[] AlphabetCodelength = new byte[0x13e];
        private HuffmanNode LiteralHuffmanTree;
        private HuffmanNode DistanceHuffmanTree;
        private byte[] CodelengthCodelength = new byte[0x13];
        private HuffmanNode CodelengthHuffmanTree;

        protected void CreateStaticCodeTrees()
        {
            int Value;

            LiteralHuffmanTree = CreateNewHuffmanTree();
            for (Value = 0x100; Value <= 0x117; Value++) AddNewCodeToHuffmanTree(LiteralHuffmanTree, 7, Value);
            for (Value = 0; Value <= 0x8f; Value++) AddNewCodeToHuffmanTree(LiteralHuffmanTree, 8, Value);
            for (Value = 0x118; Value <= 0x11f; Value++) AddNewCodeToHuffmanTree(LiteralHuffmanTree, 8, Value);
            for (Value = 0x90; Value <= 0xff; Value++) AddNewCodeToHuffmanTree(LiteralHuffmanTree, 9, Value);
            DistanceHuffmanTree = CreateNewHuffmanTree();
            for (Value = 0; Value <= 0x1f; Value++) AddNewCodeToHuffmanTree(DistanceHuffmanTree, 5, Value);
        }

        protected void FreeStaticCodeTrees()
        {
            FreeHuffmanTree(ref LiteralHuffmanTree);
            FreeHuffmanTree(ref DistanceHuffmanTree);
        }

        protected int DecodeValue(HuffmanNode ActualNode)
        {
            do
            {
                ActualNode = ActualNode.next[ReadBit()];
            }
            while (ActualNode.value == 0xffff && !SI_BREAK);

            if (SI_BREAK)
                SI_ERROR = 0x4000;

            return ActualNode.value;
        }

        protected void ReadDynamicCodeTrees(int CodelengthNumber, int LiteralNumber, int DistanceNumber)
        {
            int CodeValue;
            byte Lengthcode, CodeLength;
            int RepeatAmount;
            byte RepeatValue = 0;
            byte ActualCodeLength, HighestCodeLength, Actual2, Highest2;
            bool BuildSuccess = false;

            /* Read codelength codelengths (first tree) */
            CodelengthCodelength = new byte[0x13];
            HighestCodeLength = 0;
            for (CodeValue = 0; CodeValue <= CodelengthNumber - 0x1; CodeValue++)
            {
                if (!SI_BREAK)
                {
                    ActualCodeLength = (byte)ReadBits(3);
                    if (ActualCodeLength > HighestCodeLength) HighestCodeLength = ActualCodeLength;
                    CodelengthCodelength[CodelengthOrder[CodeValue]] = ActualCodeLength;
                }
            }

            if (SI_BREAK)
            {
                SI_ERROR = 0x4000 /* SI_USERBREAK */;
                CodelengthCodelength = null;
                return;
            }

            /* Build up tree */
            CodelengthHuffmanTree = CreateNewHuffmanTree();
            for (CodeLength = 0x1; CodeLength <= 0xf; CodeLength++)
            {
                for (CodeValue = 0; CodeValue <= 0x12; CodeValue++)
                {
                    if (CodeLength == CodelengthCodelength[CodeValue])
                        BuildSuccess = AddNewCodeToHuffmanTree(CodelengthHuffmanTree, CodeLength, CodeValue);
                }
            }

            CodelengthCodelength = null;
            if (BuildSuccess)
            {
                if (HighestCodeLength == 0) HighestCodeLength += 1;
                BuildSuccess = !AddNewCodeToHuffmanTree(CodelengthHuffmanTree, HighestCodeLength, 0);
                if (!BuildSuccess)
                {
                    SI_ERROR = (SI_ERROR & 0x7fc3) + 0x8024;
                    return;
                }
            }
            else
            {
                SI_ERROR = (SI_ERROR & 0x7fc3) + 0x8034;
                return;
            }

            /* Real literal + distance (alphabet) codelengths */
            AlphabetCodelength = new byte[0x13e];
            RepeatAmount = 0;
            HighestCodeLength = 0;
            Highest2 = 0;
            for (CodeValue = 0; CodeValue <= LiteralNumber + DistanceNumber - 0x1; CodeValue++)
            {
                if (!SI_BREAK)
                {
                    if (RepeatAmount == 0)
                    {
                        Lengthcode = (byte)DecodeValue(CodelengthHuffmanTree);
                        if (Lengthcode < 0x10)
                        {
                            AlphabetCodelength[CodeValue] = Lengthcode;
                            if (CodeValue < LiteralNumber)
                            {
                                if (Lengthcode > HighestCodeLength)
                                    HighestCodeLength = Lengthcode;
                            }
                            else
                            {
                                if (Lengthcode > Highest2)
                                    Highest2 = Lengthcode;
                            }
                        }
                        else if (Lengthcode == 0x10)
                        {
                            RepeatAmount = 0x2 + ReadBits(2);
                            RepeatValue = AlphabetCodelength[CodeValue - 0x1];
                            AlphabetCodelength[CodeValue] = RepeatValue;
                        }
                        else if (Lengthcode == 0x11)
                        {
                            RepeatAmount = 0x2 + ReadBits(3);
                            RepeatValue = 0;
                            AlphabetCodelength[CodeValue] = RepeatValue;
                        }
                        else if (Lengthcode == 0x12)
                        {
                            RepeatAmount = 0xa + ReadBits(7);
                            RepeatValue = 0;
                            AlphabetCodelength[CodeValue] = RepeatValue;
                        }

                        if (SI_BREAK)
                        {
                            SI_ERROR = 0x4000 /* SI_USERBREAK */;
                            AlphabetCodelength = null;
                            FreeHuffmanTree(ref CodelengthHuffmanTree);
                            return;
                        }
                    }
                    else
                    {
                        AlphabetCodelength[CodeValue] = RepeatValue;
                        RepeatAmount -= 1;
                    }
                }
            }

            /* Free huffman tree for codelength resolving (not needed anymore) */
            FreeHuffmanTree(ref CodelengthHuffmanTree);

            /* Build up literal tree */
            LiteralHuffmanTree = CreateNewHuffmanTree();
            for (CodeLength = 0x1; CodeLength <= 0xf; CodeLength++)
            {
                for (CodeValue = 0; CodeValue <= LiteralNumber - 1; CodeValue++)
                {
                    if (CodeLength == AlphabetCodelength[CodeValue])
                        BuildSuccess = AddNewCodeToHuffmanTree(LiteralHuffmanTree, CodeLength, CodeValue);
                }
            }
            if (BuildSuccess)
            {
                if (HighestCodeLength == 0)
                    HighestCodeLength += 1;

                BuildSuccess = !AddNewCodeToHuffmanTree(LiteralHuffmanTree, HighestCodeLength, 0);
                if (!BuildSuccess)
                {
                    SI_ERROR = (SI_ERROR & 0x7fc3) + 0x8028;
                    FreeHuffmanTree(ref LiteralHuffmanTree);
                    AlphabetCodelength = null;
                    return;
                }
            }
            else
            {
                SI_ERROR = (SI_ERROR & 0x7fc3) + 0x8038;
                FreeHuffmanTree(ref LiteralHuffmanTree);
                AlphabetCodelength = null;
                return;
            }

            /* Build up distance tree */
            DistanceHuffmanTree = CreateNewHuffmanTree();
            for (CodeLength = 0x1; CodeLength <= 0xf; CodeLength++)
            {
                for (CodeValue = 0; CodeValue <= DistanceNumber - 1; CodeValue++)
                {
                    if (CodeLength == AlphabetCodelength[CodeValue + LiteralNumber])
                        BuildSuccess = AddNewCodeToHuffmanTree(DistanceHuffmanTree, CodeLength, CodeValue);
                }
            }

            if (BuildSuccess)
            {
                if (Highest2 == 0)
                    Highest2 += 1;

                BuildSuccess = !AddNewCodeToHuffmanTree(DistanceHuffmanTree, Highest2, 0);
                if (!BuildSuccess)
                {
                    SI_ERROR = (SI_ERROR & 0x7fc3) + 0x802c;
                    FreeHuffmanTree(ref DistanceHuffmanTree);
                    AlphabetCodelength = null;
                    return;
                }
            }
            else
            {
                SI_ERROR = (SI_ERROR & 0x7fc3) + 0x803c;
                FreeHuffmanTree(ref DistanceHuffmanTree);
                AlphabetCodelength = null;
                return;
            }

            AlphabetCodelength = null;
        }

        protected void FreeDynamicCodeTrees()
        {
            FreeHuffmanTree(ref LiteralHuffmanTree);
            FreeHuffmanTree(ref DistanceHuffmanTree);
        }

        /* лллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллл */
        /* лл DEFLATE-BLOCK-HANDLING block лллллллллллллллллллллллллллллллллллл */
        /* лллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллллл */

        private const int LiteralAlphabetLengthOffset = 0x101;
        private const byte DistanceAlphabetLengthOffset = 0x01;
        private const byte LengthcodeAlphabetLengthOffset = 0x04;

        protected void OutputByte(byte b)
        {
            SI_WINDOW[SI_POSITION] = b;
            SI_POSITION++;
            if (SI_POSITION == WSIZE)
            {
                SI_WRITE(WSIZE);
                SI_POSITION = 0;
            }
        }

        protected void CopyBytes(int distance, int length)
        {
            while (length > 0)
            {
                OutputByte(SI_WINDOW[(SI_POSITION + 0x8000 - distance) & 0x7fff]);
                length--;
            }
        }

        public void SI_INFLATE()
        {
            byte LastBlock;
            int CodelengthNumber, LiteralNumber, DistanceNumber;
            int Literal;
            int Length;
            int Distance;

            // Force ReadBits to read a byte first
            BitNumber = 8;

            /* Set sliding window position to the start */
            SI_POSITION = 0;
            SI_ERROR = 0;
            AllocateStaticTables();
            SI_WINDOW = new byte[WSIZE];
            do
            {
                LastBlock = ReadBit();
                int BlockType = (byte)ReadBits(2);

                // Decompress the specific block
                if (BlockType == 0)
                {
                    // ignore all bits till next byte
                    BitNumber = 8;
                    Length = SI_READ() + SI_READ() * 0x100;
                    Distance = SI_READ() + SI_READ() * 0x100;
                    if ((Length ^ Distance) != 0xffff)
                    {
                        SI_ERROR = 0x8020;
                    }
                    else
                    {
                        while ((Length > 0) && ((SI_ERROR & 0xc000) == 0))
                        {
                            Literal = SI_READ();
                            OutputByte((byte)Literal);
                            Length -= 1;
                        }
                    }
                }
                else if (BlockType == 1)
                {
                    CreateStaticCodeTrees();
                    if ((SI_ERROR & 0x8000) == 0)
                    {
                        do
                        {
                            Literal = DecodeValue(LiteralHuffmanTree);
                            if ((SI_ERROR & 0xc000) != 0)
                            {
                                Literal = 0x100;
                            }
                            else
                            {
                                if (Literal < 0x100)
                                {
                                    OutputByte((byte)Literal);
                                }
                                else if (Literal == 0x100)
                                {
                                    // No-op
                                }
                                else if (Literal <= 0x11d)
                                {
                                    Length = LengthcodeValueOffset[Literal] + ReadBits(LengthcodeValueExtrabits[Literal]);
                                    Distance = DecodeValue(DistanceHuffmanTree);
                                    if (Distance > 0x1d)
                                    {
                                        SI_ERROR = 0x8000;
                                        Literal = 0x100;
                                    }
                                    else
                                    {
                                        Distance = DistancecodeValueOffset[Distance] + ReadBits(DistancecodeValueExtrabits[Distance]);
                                        CopyBytes(Distance, Length);
                                    }
                                }
                                else
                                {
                                    SI_ERROR = 0x8000;
                                    Literal = 0x100;
                                }
                            }
                        }
                        while (Literal != 0x100);
                    }

                    FreeStaticCodeTrees();
                }
                else if (BlockType == 2)
                {
                    LiteralNumber = LiteralAlphabetLengthOffset + ReadBits(5);
                    DistanceNumber = DistanceAlphabetLengthOffset + ReadBits(5);
                    CodelengthNumber = LengthcodeAlphabetLengthOffset + ReadBits(4);
                    ReadDynamicCodeTrees(CodelengthNumber, LiteralNumber, DistanceNumber);
                    if ((SI_ERROR & 0x8000) == 0)
                    {
                        do
                        {
                            Literal = DecodeValue(LiteralHuffmanTree);
                            if ((SI_ERROR & 0xc000) != 0) Literal = 0x100;
                            else
                            {
                                if (Literal < 0x100)
                                {
                                    OutputByte((byte)Literal);
                                }
                                else if (Literal == 0x100)
                                {
                                    // No-op
                                }
                                else if (Literal <= 0x11d)
                                {
                                    Length = LengthcodeValueOffset[Literal] + ReadBits(LengthcodeValueExtrabits[Literal]);
                                    Distance = DecodeValue(DistanceHuffmanTree);
                                    Distance = DistancecodeValueOffset[Distance] + ReadBits(DistancecodeValueExtrabits[Distance]);
                                    CopyBytes(Distance, Length);
                                }
                                else
                                {
                                    SI_ERROR = 0x8000;
                                    Literal = 0x100;
                                }
                            }
                        }
                        while (Literal != 0x100);
                    }

                    if (((SI_ERROR & 0x8000) == 0) && (SI_ERROR != 0x8000))
                    {
                        FreeDynamicCodeTrees();
                    }
                }
            }
            while (LastBlock != 1);

            if (((SI_ERROR & 0xc000) == 0) && (SI_POSITION > 0))
                SI_WRITE(SI_POSITION);

            DeAllocateStaticTables();
            SI_WINDOW = null;
        }
    }
}
