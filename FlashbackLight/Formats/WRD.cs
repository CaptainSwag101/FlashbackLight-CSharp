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
            if (externalStrings)
            {
                // Strings are stored in the "(current spc name)_text_(region).spc" file,
                // within an STX file with the same name as the current WRD file.
                string textSPCName = spcName.Insert(spcName.LastIndexOf('.'), string.Format("_text_{0}", MainForm.RegionString));
                string stxName = wrdName.Replace(".wrd", ".stx");
                byte[] spcData = File.ReadAllBytes(textSPCName);
                SPC textSPC = new SPC(spcData, textSPCName);
                foreach (SPCEntry entry in textSPC.Entries)
                {
                    if (entry.Filename == stxName)
                    {
                        Strings = ((STX)entry.Contents).Strings;
                        break;
                    }
                }
            }
            /*
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
                    if ((byte)stream.device()->peek(1).at(0) >= 0x80)
                    {
                        stream >> stringLen;
                    }
                    else
                    {
                        uchar c;
                        stream >> c;
                        stringLen = c;
                    }
                    stringLen += 2; // Null terminator

                    QChar* stringData = new QChar[stringLen / 2];

                    for (int j = 0; j < (stringLen / 2); ++j)
                    {
                        QChar chr = 0;
                        stream >> chr;
                        stringData[j] = chr;

                        // We can't always trust stringLen apparently, so break if we've hit a null terminator.
                        if (chr == QChar(0))
                            break;
                    }

                    QString string = QString(stringData);
                    string.replace('\r', "\\r");
                    string.replace('\n', "\\n");
                    result.strings.append(string);
                    delete[] stringData;
                }
            }
            */
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
