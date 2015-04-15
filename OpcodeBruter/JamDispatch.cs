using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Bea;

namespace OpcodeBruter
{
    public class JamDispatch
    {
        public virtual int StructureOffset { get; private set; }
        // Not actual padding - just two vtable entries nulled.
        public virtual int PaddingSize { get { return 8; } }
        public int CalculateDispatcherFn() { return _dispatcher; }
        public int CalculateCheckerFn()    { return _checker; }
        public int CalculateConnectionFn() { return _connIndex; }

        private int _dispatcher;
        private int _checker;
        private int _connIndex;
        private JamGroup _groupName;

        public JamGroup GetGroup() { return _groupName; }

        public JamDispatch(int nameOffset)
        {
            Program.BaseStream.Seek(nameOffset, SeekOrigin.Begin);
            
            var groupName = String.Empty;
            while (Program.ClientStream.PeekChar() != '\0')
                groupName += Program.ClientStream.ReadChar();

            if (!Enum.TryParse(groupName, out _groupName))
                Logger.WriteConsoleLine(@"Found JAM Group ""{0}"", which is not defined in the JamGroup enum!", groupName);

            // Increment the name offset to look it up in the binary
            //! TODO This offset can change, always check it.
            nameOffset += 0x400E00; // And skip the name pointer again
            var structureOffsetsCandidates = Program.ClientBytes.FindPattern(BitConverter.GetBytes(nameOffset), 0xFF);
            if (structureOffsetsCandidates.Count == 0)
                Console.WriteLine("No JAMRecord structure found for group {0}", GetGroup());
            else
            {
                StructureOffset = (int)structureOffsetsCandidates[0];
                Program.BaseStream.Seek(StructureOffset + 4, SeekOrigin.Begin);
                _checker = Program.ClientStream.ReadInt32() - 0x400C00;
                Program.ClientStream.ReadBytes(PaddingSize + 4);
                _dispatcher = Program.ClientStream.ReadInt32() - 0x400C00;
                _connIndex = Program.ClientStream.ReadInt32();
                if (_connIndex > 0)
                    _connIndex -= 0x400C00;
            }
        }
    }
}
