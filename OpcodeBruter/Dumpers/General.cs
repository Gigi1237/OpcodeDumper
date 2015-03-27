using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcodeBruter.Dumpers
{
    public abstract class OpcodeInfo
    {
        public string Name { protected set; get; }
        public UInt32 Opcode { private set; get; }
        public double Certianty { protected set; get; }

        public OpcodeInfo(string name, UInt32 opcode, double certianty = 0.0f)
        {
            Name = name;
            Opcode = opcode;
            Certianty = certianty;
        }

        public abstract void FormatName();
        public abstract string getPrintString();
    }
}
