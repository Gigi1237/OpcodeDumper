using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Bea;

namespace OpcodeBruter.Dumpers
{
    /// <summary>
    /// Description of CliOpcodes.
    /// </summary>
    public class SMSG
    {
        private static List<SMSGInfo> smsgInfo = new List<SMSGInfo>();
        public static int opcodeCount { private set; get; }

        public static void Dump()
        {
            var jamGroupCount = new Dictionary<JamGroup, uint>();

            Logger.WriteLine();
            Logger.WriteLine("Dumping SMSG opcodes...");
            Logger.WriteLine("+---------------+-------------+-------------+--------------------+---------+------------+");
            Logger.WriteLine("|     Opcode    |  JAM Parser | Jam Handler |     Group Name     | ConnIdx | CONFIDENCE |");
            Logger.WriteLine("+---------------+-------------+-------------+--------------------+---------+------------+");

            if (Config.SpecificOpcodeValue == 0xBADD)
            {
                for (uint i = 0; i < 0x1FFF; ++i)
                    if (DumpOpcode(i, jamGroupCount))
                        ++opcodeCount;
            }
            else if (DumpOpcode(Config.SpecificOpcodeValue, jamGroupCount))
                ++opcodeCount;

            Logger.WriteLine("+---------------+-------------+-------------+--------------------+---------+");
            Logger.WriteLine(@"Dumped {0} SMSG JAM opcodes.", opcodeCount);
            for (var i = 0; i < jamGroupCount.Count; ++i)
                Logger.WriteLine("Dumped {0} SMSG {1} opcodes.", jamGroupCount.Values.ElementAt(i), jamGroupCount.Keys.ElementAt(i).ToString());

            smsgInfo = smsgInfo.OrderBy(x => x.Name).ToList();
        }
        
        private static bool DumpOpcode(uint opcode, Dictionary<JamGroup, uint> jamGroupCount)
        {
            foreach (var dispatcherPair in Program.Dispatchers)
            {
                if (dispatcherPair.Key == JamGroup.None)
                    continue;

                var dispatcher = dispatcherPair.Value;
                if (dispatcher.CalculateCheckerFn() == 0)
                    continue;

                int offset = dispatcher.StructureOffset;
                if (offset <= 0)
                    continue;

                int checkerFn    = dispatcher.CalculateCheckerFn();
                int connectionFn = dispatcher.CalculateConnectionFn();
                int dispatcherFn = dispatcher.CalculateDispatcherFn();

                Program.Environment.Reset();
                Program.Environment.Push(opcode);
                Program.Environment.Execute(checkerFn, Program.Disasm, false);
                if (Program.Environment.Eax.Value == 0)
                    continue;

                var connIndex = 0u;
                if (connectionFn != 0)
                {
                    Program.Environment.Reset();
                    Program.Environment.Push();
                    Program.Environment.Push();
                    Program.Environment.Push();
                    Program.Environment.Push(opcode);
                    Program.Environment.Push();
                    Program.Environment.Execute(connectionFn, Program.Disasm, false);
                    if (Program.Environment.Eax.Al == 0)
                    {
                        var requiresInstanceConnectionFn = Program.Environment.GetCalledOffsets()[0] - 0x400C00;

                        Program.Environment.Reset();
                        Program.Environment.Push(opcode);
                        Program.Environment.Execute(requiresInstanceConnectionFn, Program.Disasm, false);
                        connIndex = Program.Environment.Eax.Value;
                    }
                }

                Program.Environment.Reset();
                Program.Environment.Execute(dispatcherFn, Program.Disasm, false);
                var calleeOffset = Program.Environment.GetCalledOffsets()[0] - 0x400C00;

                Program.Environment.Reset();
                Program.Environment.Push();
                Program.Environment.Push((ushort)0);
                Program.Environment.Push((ushort)opcode);
                Program.Environment.Push();
                Program.Environment.Push();
                Program.Environment.Execute(calleeOffset, Program.Disasm, false);
                var jamData = Program.Environment.GetCalledOffsets();
                if (jamData.Length < 2)
                    continue;

                var handler = jamData[0];
                var parser  = jamData[1];
                switch (dispatcher.GetGroup())
                {
                    case JamGroup.Client:
                    case JamGroup.ClientChat:
                    case JamGroup.ClientGuild:
                    case JamGroup.ClientQuest:
                        handler = jamData[1];
                        parser  = jamData[2];
                        break;
                }

                if (!jamGroupCount.ContainsKey(dispatcher.GetGroup()))
                    jamGroupCount.Add(dispatcher.GetGroup(), 0);
                jamGroupCount[dispatcher.GetGroup()] += 1;

                Logger.WriteLine("| {1} (0x{0:X4}) |  0x{2:X8} |  0x{3:X8} | {4} | {5} |   {6:F4}   | {7}",
                                 opcode,
                                 opcode.ToString().PadLeft(4),
                                 handler,
                                 parser,
                                 dispatcher.GetGroup().ToString().PadLeft(18),
                                 connIndex.ToString().PadLeft(7),
                                 Program.FuncDiff != null ? Program.FuncDiff.getCertianty(parser) : 0,
                                 Opcodes.GetOpcodeNameForServer(opcode, parser));

                smsgInfo.Add(new SMSGInfo(Opcodes.GetOpcodeNameForServer(opcode, parser), opcode, handler, parser, dispatcher.GetGroup().ToString(),
                    (Program.FuncDiff != null ? Program.FuncDiff.getCertianty(parser) : 0.0f)));
                return true;
            }
            return false;
        }
    }

    public class SMSGInfo : OpcodeInfo
    {
        public UInt32 Handler { private set; get; }
        public UInt32 Parser { private set; get; }
        public string Dispatcher { private set; get; }

        public SMSGInfo(string name, UInt32 opcode, UInt32 handler, UInt32 parser, string dispatcher, double certianty = 0.0f) : base(name, opcode, certianty)
        {
            Handler = handler;
            Parser = parser;
            Dispatcher = dispatcher;
        }

        public override void FormatName()
        {
            if (!Name.StartsWith("SMSG"))
            {
                StringBuilder nameBuilder = new StringBuilder("SMSG");
                Name = Name.Remove(0, 6);
                for (int i = 0; i < Name.Length; i++) // Removes 'Client' prefix from name
                {
                    if (Char.IsUpper(Name[i]))
                    {
                        nameBuilder.AppendFormat("_{0}", char.ToUpper(Name[i]));
                    }
                    else
                        nameBuilder.Append(char.ToUpper(Name[i]));
                }

                Name = nameBuilder.ToString();
            }
        }

        public override string getPrintString()
        {
            throw new NotImplementedException();
        }
    }
}
