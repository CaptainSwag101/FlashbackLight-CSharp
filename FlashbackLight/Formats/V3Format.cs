using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashbackLight.Formats
{
    abstract class V3Format
    {
        public abstract byte[] ToBytes();
    }
}
