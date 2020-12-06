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
        ReadReg = 0x0080,
        ReadRegAck = 0x0081,
        WriteReg = 0x0082,
        WriteRegAck = 0x0083,
        ReadMem = 0x0084,
        ReadMemAck = 0x0085,
        WriteMem = 0x0086,
        WrireMemAck = 0x0087,
        Invalid = 0x88,
    }
}