using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigeVision.Core.Enums
{
    public enum GvcpCommandType
    {
        Discovery = 0x0002,
        Read = 0x0080,
        ReadAck = 0x0081,
        Write = 0x0082,
        WriteAck = 0x0083,
        Invalid = 0x84,
    }
}