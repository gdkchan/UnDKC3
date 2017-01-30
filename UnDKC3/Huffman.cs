using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnDKC3
{
    class Huffman
    {
        private byte[] Data;

        private int[] SymbolLUT;

        private int TreeRootAddr;
        private int TreeBaseAddr;

        private string CharSet;

        public Huffman(byte[] Data, int TreeRootAddress, string CharSet)
        {
            this.Data = Data;

            SymbolLUT = new int[0x200];

            TreeRootAddr = TreeRootAddress;
            TreeBaseAddr = TreeRootAddress + 2;

            this.CharSet = CharSet;

            //Traverse entire tree and build a LUT used to quickly compress data
            Stack<int> TreeStack = new Stack<int>();

            int CurrNode = GetUShort(TreeRootAddr);

            int Code = 0;
            int Depth = 0;

            while (true)
            {
                TreeStack.Push(CurrNode);

                bool LeftNode = (Code & 1) != 0;

                if (LeftNode)
                    CurrNode = GetUShort(TreeBaseAddr + CurrNode);
                else
                    CurrNode = GetUShort(TreeBaseAddr + CurrNode + 2);

                if (CurrNode == 0)
                {
                    int Symbol = Data[TreeBaseAddr + TreeStack.Pop() - 1];

                    SymbolLUT[Symbol * 2 | 0] = Depth;
                    SymbolLUT[Symbol * 2 | 1] = Code >> 1;

                    do
                    {
                        if (TreeStack.Count == 0) return;

                        CurrNode = TreeStack.Pop();

                        Code >>= 1;
                        Depth--;
                    }
                    while ((Code & 1) != 0);

                    Code |= 1;
                }
                else
                {
                    Code <<= 1;
                    Depth++;
                }
            }
        }

        public byte[] Compress(string Input)
        {
            using (MemoryStream MS = new MemoryStream())
            {
                CurrWord = 0;
                CurrBits = 0;

                for (int i = 0; i < 5; i++)
                {
                    Input = Input.Replace(string.Format("\\{0}\r\n", i), ((char)i).ToString()); //Windows style
                    Input = Input.Replace(string.Format("\\{0}\n",   i), ((char)i).ToString()); //Linux style
                }

                string[] Lines = Utils.ToLines(Input);

                foreach (string Line in Lines)
                {
                    for (int i = 0; i < Line.Length; i++)
                    {
                        int Symbol = Line[i];

                        if (Symbol > 0x1f) Symbol = (byte)CharSet.IndexOf(Line[i], 0x20);

                        WriteSymbol(MS, Symbol);
                    }

                    WriteSymbol(MS, 0);
                }

                if (CurrBits != 0)
                {
                    CurrWord <<= (16 - CurrBits);

                    MS.WriteByte((byte)(CurrWord >> 0));
                    MS.WriteByte((byte)(CurrWord >> 8));
                }

                return MS.ToArray();
            }
        }

        private int CurrWord;
        private int CurrBits;

        private void WriteSymbol(MemoryStream MS, int Symbol)
        {
            int Bits = SymbolLUT[Symbol * 2 | 0];
            int Code = SymbolLUT[Symbol * 2 | 1];

            if (CurrBits + Bits <= 16)
            {
                CurrWord <<= Bits;
                CurrWord |= Code;
                CurrBits += Bits;
            }
            else
            {
                int SplitBits = 16 - CurrBits;
                int SplitCode = Code >> (Bits - SplitBits);

                CurrWord <<= SplitBits;
                CurrWord |= SplitCode;

                MS.WriteByte((byte)(CurrWord >> 0));
                MS.WriteByte((byte)(CurrWord >> 8));

                CurrBits = Bits - SplitBits;
                CurrWord = Code & ((1 << CurrBits) - 1);
            }
        }

        public string Decompress(int Position, int Length)
        {
            StringBuilder SB = new StringBuilder();

            int CurrNode = GetUShort(TreeRootAddr);
            int FlagsAddr = Position;
            int FlagsMask = 0;
            int NodeFlags = 0;
            int SymbolsCount = 0;

            while (SymbolsCount < Length - 1)
            {
                if ((FlagsMask >>= 1) == 0)
                {
                    FlagsMask = 0x8000;
                    NodeFlags = GetUShort(FlagsAddr);
                    FlagsAddr += 2;
                }

                bool RightNode = (NodeFlags & FlagsMask) == 0;

                int Node = GetUShort(TreeBaseAddr + CurrNode + (RightNode ? 2 : 0));

                if (Node == 0)
                {
                    byte Symbol = Data[TreeBaseAddr + CurrNode - 1];

                    if (Symbol < 5)
                    {
                        if (Symbol == 0)
                            SB.Append("\r\n");
                        else
                            SB.Append("\\" + Symbol + "\r\n");
                    }
                    else
                    {
                        SB.Append(CharSet[Symbol]);
                    }

                    SymbolsCount++;

                    Node = GetUShort(TreeRootAddr);

                    FlagsMask <<= 1;
                }

                CurrNode = Node;
            }

            return SB.ToString();
        }

        private int GetUShort(int Index)
        {
            return
                (Data[Index + 0] << 0) |
                (Data[Index + 1] << 8);
        }
    }
}
