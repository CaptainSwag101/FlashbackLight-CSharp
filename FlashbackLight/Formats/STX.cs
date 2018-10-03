using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashbackLight.Formats
{
    class STX : V3Format
    {
        public List<string> Strings;

        public STX()
        {
            Strings = new List<string>();
        }

        public STX(byte[] bytes)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(bytes), Encoding.UTF8);
            string stxMagic = new string(reader.ReadChars(4));
            if (stxMagic != "STXT")
                throw new InvalidDataException("Error parsing STX file: Invalid magic number.");

            string lang = new string(reader.ReadChars(4));

            reader = new BinaryReader(new MemoryStream(bytes), Encoding.Unicode);
            reader.BaseStream.Seek(8, SeekOrigin.Begin);
            uint unk1 = reader.ReadUInt32();    // Table count?
            uint tableOffset = reader.ReadUInt32();
            uint unk2 = reader.ReadUInt32();
            uint tableLen = reader.ReadUInt32();
            
            Strings = new List<string>();
            for (uint s = 0; s < tableLen; s++)
            {
                reader.BaseStream.Seek(tableOffset + (8 * s), SeekOrigin.Begin);
                uint stringIndex = reader.ReadUInt32();
                uint stringOffset = reader.ReadUInt32();

                reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);
                List<char> charList = new List<char>();
                while (reader.PeekChar() != 0)
                {
                    charList.Add(reader.ReadChar());
                }
                Strings.Add(new string(charList.ToArray()));
            }
        }

        public override byte[] ToBytes()
        {
            List<byte> result = new List<byte>();



            return result.ToArray();
        }
    }
}
