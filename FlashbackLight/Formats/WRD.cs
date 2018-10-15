using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashbackLight.Formats
{
    class WRD : V3Format
    {
        public List<string> Labels;
        public List<string> Params;
        public List<string> Strings;
        public List<WRDCmd> Code;
        private bool externalStrings;

        public WRD()
        {

        }

        public WRD(byte[] bytes, string spcName, string wrdName)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(bytes), Encoding.UTF8);

            ushort stringCount = reader.ReadUInt16();
            ushort labelCount = reader.ReadUInt16();
            ushort paramCount = reader.ReadUInt16();
            ushort sublabelCount = reader.ReadUInt16();
            reader.BaseStream.Seek(4, SeekOrigin.Current);

            uint sublabelOffsetsPointer = reader.ReadUInt32();
            uint labelOffsetsPointer = reader.ReadUInt32();
            uint labelNamesPointer = reader.ReadUInt32();
            uint paramsPointer = reader.ReadUInt32();
            uint stringsPointer = reader.ReadUInt32();

            Code = new List<WRDCmd>();
            // We need at least 2 bytes for each command
            while (reader.BaseStream.Position + 1 < sublabelOffsetsPointer)
            {
                byte b = reader.ReadByte();
                if (b != 0x70) throw new InvalidDataException(string.Format("Error parsing WRD file: Expected opcode header byte 0x70, but got {0}", b));

                WRDCmd cmd = new WRDCmd();
                cmd.Opcode = reader.ReadByte();

                // Read command arguments, if any
                List<ushort> argList = new List<ushort>();
                while (reader.BaseStream.Position + 1 < sublabelOffsetsPointer)
                {
                    byte[] arg = reader.ReadBytes(2);
                    if (arg[0] == 0x70)
                    {
                        reader.BaseStream.Seek(-2, SeekOrigin.Current);
                        break;
                    }

                    argList.Add(BitConverter.ToUInt16(arg.Reverse().ToArray(), 0));
                }
                cmd.ArgData = argList.ToArray();

                Code.Add(cmd);
            }

            Labels = new List<string>();
            reader.BaseStream.Seek(labelNamesPointer, SeekOrigin.Begin);
            for (ushort l = 0; l < labelCount; l++)
            {
                Labels.Add(reader.ReadString());
                reader.ReadByte();  // Skip null terminator
            }

            Params = new List<string>();
            reader.BaseStream.Seek(paramsPointer, SeekOrigin.Begin);
            for (ushort p = 0; p < paramCount; p++)
            {
                Params.Add(reader.ReadString());
                reader.ReadByte();  // Skip null terminator
            }

            externalStrings = (stringsPointer == 0);

            // Read dialogue text strings
            Strings = new List<string>();
            if (stringCount > 0)
            {
                // If we already know that there are no strings,
                // there's no need to go through the work to find them.

                if (externalStrings)
                {
                    // Strings are stored in the "(current spc name)_text_(region).spc" file,
                    // within an STX file with the same name as the current WRD file.
                    string textSPCName = textSPCName = spcName.Insert(spcName.LastIndexOf('.'), string.Format("_text_{0}", MainForm.RegionString));
                    if (!File.Exists(textSPCName))
                    {
                        // If the first filename fails, we probably need to remove a duplicate
                        // region tag from the filename before "_text_".
                        textSPCName = textSPCName.Remove(textSPCName.LastIndexOf("_text_") - 3, 3);

                        if (!File.Exists(textSPCName))
                        {
                            // If the file still doesn't exist, it's probably not
                            // there and the strings should just be abandoned.
                            System.Windows.Forms.MessageBox.Show($"{spcName} does not have an associated .stx text file.",
                                "Missing .stx file",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Warning,
                                System.Windows.Forms.MessageBoxDefaultButton.Button1);
                            return;
                        }
                    }

                    string stxName = wrdName.Replace(".wrd", ".stx");
                    byte[] spcData = File.ReadAllBytes(textSPCName);
                    SPC textSPC = new SPC(spcData, textSPCName);

                    Strings = new STX(textSPC.Entries[stxName].Contents).Strings;
                }
                else
                {
                    reader.BaseStream.Seek(stringsPointer, SeekOrigin.Begin);
                    for (ushort i = 0; i < stringCount; ++i)
                    {
                        short stringLen = 0;

                        // The string length is a signed byte, so if it's larger than 0x7F,
                        // that means the length is actually stored in a signed short,
                        // since we can't have a negative string length.
                        // ┐(´∀｀)┌
                        if ((byte)reader.PeekChar() >= 0x80)
                        {
                            stringLen = reader.ReadInt16();
                        }
                        else
                        {
                            stringLen = reader.ReadByte();
                        }
                        stringLen += 2; // Null terminator

                        List<char> stringData = new List<char>(stringLen / 2);
                        for (int j = 0; j < (stringLen / 2); ++j)
                        {
                            char c = Convert.ToChar(reader.ReadUInt16());
                            stringData.Add(c);

                            // We can't always trust stringLen apparently, so break if we've hit a null terminator.
                            if (c == 0)
                                break;
                        }

                        string str = new string(stringData.ToArray());
                        str = str.Replace("\r", "\\r");
                        str = str.Replace("\n", "\\n");
                        Strings.Add(str);
                    }
                }
            }
        }

        public override byte[] ToBytes()
        {
            List<byte> result = new List<byte>();



            return result.ToArray();
        }
    }

    class WRDCmd
    {
        public byte Opcode;
        public ushort[] ArgData;

        public string Name
        {
            get { return NAME_LIST[Opcode]; }
        }

        public byte[] ArgTypes
        {
            get { return ARGTYPE_LIST[Opcode]; }
            
        }

        public bool IsVarLength
        {
            get
            {
                // TODO: opcode 0x02 and 0x07 might not have variable-length parameters
                if ((Opcode >= 0x01 && Opcode <= 0x03) || Opcode == 0x07)
                    return true;
                else
                    return false;
            }
        }

        /// Official command names found in game_resident/command_label.dat
        public static string[] NAME_LIST =
        {
            "FLG", "IFF", "WAK", "IFW", "SWI", "CAS", "MPF", "SPW", "MOD", "HUM",
            "CHK", "KTD", "CLR", "RET", "KNM", "CAP", "FIL", "END", "SUB", "RTN",
            "LAB", "JMP", "MOV", "FLS", "FLM", "VOI", "BGM", "SE_", "JIN", "CHN",
            "VIB", "FDS", "FLA", "LIG", "CHR", "BGD", "CUT", "ADF", "PAL", "MAP",
            "OBJ", "BUL", "CRF", "CAM", "KWM", "ARE", "KEY", "WIN", "MSC", "CSM",
            "PST", "KNS", "FON", "BGO", "LOG", "SPT", "CDV", "SZM", "PVI", "EXP",
            "MTA", "MVP", "POS", "ICO", "EAI", "COL", "CFP", "CLT=", "R=", "PAD=",
            "LOC", "BTN", "ENT", "CED", "LBN", "JMN"
        };

        public static byte[][] ARGTYPE_LIST = new[]
        {
            new byte[] {0,0}, new byte[] {0,0,0}, new byte[] {0,0,0}, new byte[] {0,0,1}, new byte[] {0}, new byte[] {1}, new byte[] {0,0,0}, new byte[] {}, new byte[] {0,0,0,0}, new byte[] {0},
            new byte[] {0}, new byte[] {0,0}, new byte[] {}, new byte[] {}, new byte[] {0,0,0,0,0}, new byte[] {}, new byte[] {0,0}, new byte[] {}, new byte[] {0,0}, new byte[] {},
            new byte[] {3}, new byte[] {0}, new byte[] {0,0}, new byte[] {0,0,0,0}, new byte[] {0,0,0,0,0,0}, new byte[] {0,0}, new byte[] {0,0,0}, new byte[] {0,0}, new byte[] {0,0}, new byte[] {0},
            new byte[] {0,0,0}, new byte[] {0,0,0}, new byte[] {}, new byte[] {0,1,0}, new byte[] {0,0,0,0,0}, new byte[] {0,0,0,0}, new byte[] {0,0}, new byte[] {0,0,0,0,0}, new byte[] {}, new byte[] {0,0,0},
            new byte[] {0,0,0}, new byte[] {0,0,0,0,0,0,0,0}, new byte[] {0,0,0,0,0,0,0}, new byte[] {0,0,0,0,0}, new byte[] {0}, new byte[] {0,0,0}, new byte[] {0,0}, new byte[] {0,0,0,0}, new byte[] {}, new byte[] {},
            new byte[] {0,0,1,1,1}, new byte[] {0,1,1,1,1}, new byte[] {1,1}, new byte[] {0,0,0,0,0}, new byte[] {}, new byte[] {0}, new byte[] {0,0,0,0,0,0,0,0,0,0}, new byte[] {0,0,0,0}, new byte[] {0}, new byte[] {0},
            new byte[] {0}, new byte[] {0,0,0}, new byte[] {0,0,0,0,0}, new byte[] {0,0,0,0}, new byte[] {0,0,0,0,0,0,0,0,0,0}, new byte[] {0,0,0}, new byte[] {0,0,0,0,0,0,0,0,0}, new byte[] {0}, new byte[] {}, new byte[] {0},
            new byte[] {2}, new byte[] {}, new byte[] {}, new byte[] {}, new byte[] {1}, new byte[] {1}
        };
    }
}
