using System;
using System.Collections.Generic;
using System.IO;

namespace UnDKC3
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("UnDKC3 - Donkey Kong Country 3 Text Editor by gdkchan");
            Console.WriteLine("Version 0.1.2");
            Console.WriteLine(string.Empty);
            Console.ResetColor();

            string Lang    = string.Empty;
            string ROMFile = string.Empty;
            string Folder  = string.Empty;

            if (args.Length == 4)
            {
                Lang    = args[1];
                ROMFile = args[2];
                Folder  = args[3];
            }
            else if (args.Length == 3)
            {
                Lang    = "en";
                ROMFile = args[1];
                Folder  = args[2];
            }

            if (File.Exists(ROMFile))
            {
                byte[] ROM = File.ReadAllBytes(ROMFile);

                int TreeOffset = 0;
                int PosOffset = 0;
                int PtrAddr = 0;
                int EndAddr = 0;

                string CharSet = string.Empty;

                string[] CharSets = Utils.ToLines(Properties.Resources.CharSets);

                switch (Lang.ToLower())
                {
                    case "en":
                        TreeOffset = 0;
                        PosOffset = 0;
                        PtrAddr = 0x379df5;
                        EndAddr = 0x379e45;

                        CharSet = CharSets[0];

                        break;

                    case "fr":
                        TreeOffset = 0x300;
                        PosOffset = 0x5393;
                        PtrAddr = 0x379e45;
                        EndAddr = 0x379e95;

                        CharSet = CharSets[1];

                        break;
                }

                Huffman Huff = new Huffman(ROM, 0x379ee5 + TreeOffset, CharSet);

                switch (args[0].ToLower())
                {
                    case "-extract":
                        string OutDir = Folder;

                        Directory.CreateDirectory(OutDir);

                        while (PtrAddr < EndAddr)
                        {
                            int Length   = Utils.GetUShort(ROM, PtrAddr + 0);
                            int Position = Utils.GetUShort(ROM, PtrAddr + 2) | 0x3a0000;

                            Position += PosOffset;

                            PtrAddr += 4;

                            string Text = Huff.Decompress(Position, Length);

                            string FileName = Path.Combine(OutDir, Position.ToString("x6") + ".txt");

                            Console.WriteLine(string.Format("Extracting (pointer at 0x{0:x6}) to file \"{1}\"...", PtrAddr - 2, FileName));

                            File.WriteAllText(FileName, Text);
                        }
                        break;

                    case "-insert":
                        string InDir = Folder;

                        List<int> PosTbl = new List<int>();

                        for (int i = PtrAddr; i < EndAddr; i += 4)
                        {
                            PosTbl.Add((Utils.GetUShort(ROM, i + 2) | 0x3a0000) + PosOffset);
                        }

                        PosTbl.Sort();

                        while (PtrAddr < EndAddr)
                        {
                            int Length   = Utils.GetUShort(ROM, PtrAddr + 0);
                            int Position = Utils.GetUShort(ROM, PtrAddr + 2) | 0x3a0000;

                            Position += PosOffset;

                            PtrAddr += 4;

                            string FileName = Path.Combine(InDir, Position.ToString("x6") + ".txt");

                            if (!File.Exists(FileName))
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Warning: File \"" + FileName + "\" not found!");
                                Console.ResetColor();

                                continue;
                            }

                            byte[] Data = Huff.Compress(File.ReadAllText(FileName));

                            int Index = PosTbl.IndexOf(Position);

                            if (Index + 1 < PosTbl.Count)
                            {
                                if (Position + Data.Length > PosTbl[Index + 1])
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("Warning: File \"" + FileName + "\" is too big! Skipping...");
                                    Console.ResetColor();

                                    continue;
                                }
                            }

                            Console.WriteLine(string.Format("Inserting (pointer at 0x{0:x6}) from file \"{1}\"...", PtrAddr - 2, FileName));

                            Buffer.BlockCopy(Data, 0, ROM, Position, Data.Length);
                        }

                        File.WriteAllBytes(ROMFile, ROM);
                        break;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Usage:");
                Console.WriteLine(string.Empty);
                Console.ResetColor();

                Console.WriteLine("undkc3 [-extract|-insert] (en|fr) rom.smc folder");
                Console.WriteLine(string.Empty);

                Console.WriteLine("-extract  Extracts texts from the ROM");
                Console.WriteLine("-insert  Inserts texts into the ROM");
                Console.WriteLine("en  Used to handle english texts (default, optional)");
                Console.WriteLine("fr  Used to handle french texts (optional)");
                Console.WriteLine("rom.smc  The ROM file (U or E, J is not supported)");
                Console.WriteLine("folder  Output folder (when extracting) or input folder (when inserting)");
                Console.WriteLine(string.Empty);

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Examples:");
                Console.WriteLine(string.Empty);
                Console.ResetColor();

                Console.WriteLine("udkc3 -extract dkc3.smc english_texts");
                Console.WriteLine("udkc3 -insert dkc3.smc english_texts");
                Console.WriteLine("udkc3 -extract fr dkc3.smc french_texts");
                Console.WriteLine("udkc3 -insert fr dkc3.smc french_texts");
            }
        }
    }
}
