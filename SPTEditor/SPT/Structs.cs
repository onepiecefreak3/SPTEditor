using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPTEditor.IO;

namespace SPTEditor.SPT.Structs
{
    public class SPTHeader
    {
        [Length(4)]
        public string magic = " TPS";
        public short version = 0x0100;
        public short partitionCount;
        public short biggestPartitionSize;
        public ushort XORKey = 0x0000;
    }

    public class PartitionInfo
    {
        public int absOffset;
        public int codeCount;
        public short flags1;
        public short flags2;
    }

    public class CodeInfo
    {
        public ushort code;
        public List<ushort> arguments = new List<ushort>();
    }
}
