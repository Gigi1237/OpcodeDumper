using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Bea;

namespace OpcodeBruter.Dumpers
{
    public class CMSG
    {
        //! DOUBLECHECK THIS FOR ANY BUILD
        private static byte[] Pattern = new byte[] {
            0x55,                             // push    ebp
            0x8B, 0xEC,                       // mov     ebp, esp
            0x56,                             // push    esi
            0x8B, 0xF1,                       // mov     esi, ecx
            0x8B, 0x4D, 0x08,                 // mov     ecx, [ebp + 8]
            0x68, 0xFF, 0xFF, 0x00, 0x00,     // push    <opcode>
            0xE8, 0xFF, 0xFF, 0xFF, 0xFF      // call    CDataStore::PutInt32
        };

        private static List<CMSGInfo> cmsgInfo = new List<CMSGInfo>();
        public static int opcodeCount { private set; get; }

        public static void Dump(uint specificOpcode = 0xBADD)
        {
            if (specificOpcode != 0xBADD)
            {
                Pattern[11] = (byte)((specificOpcode & 0xFF00) >> 8);
                Pattern[10] = (byte)(specificOpcode & 0x00FF);
                Console.WriteLine(BitConverter.ToString(Pattern).Replace('-', ' '));
            }

            var patternOffsets = Program.ClientBytes.FindPattern(Pattern, 0xFF);
            var callIndex = (uint)(Array.IndexOf<byte>(Pattern, 0xE8) + 0x400C00);

            Logger.WriteLine();
            Logger.WriteLine("Dumping CMSG opcodes...");
            Logger.WriteLine("Found {0} CMSGs candidates. Dumping, this may take a while...", patternOffsets.Count);
            Logger.WriteLine("+---------------+------------+------------+------------+");
            Logger.WriteLine("|    Opcode     |   vTable   |   CliPut   | CONFIDENCE |");
            Logger.WriteLine("+---------------+------------+------------+------------+");

            foreach (var currPatternOffset in patternOffsets)
            {
                Program.BaseStream.Seek(currPatternOffset, SeekOrigin.Begin);
                var bytes = Program.ClientStream.ReadBytes(Pattern.Length);

                var callOffset = BitConverter.ToInt32(bytes, 15);

                // False positive check
                int PutUInt32 = 0;
                switch (Program.ClientBuild.BuildNumber)
                {
                    case 19324:
                        PutUInt32 = 0x0040FD64;
                        break;
                    case 19702:
                        PutUInt32 = 0x004110E6;
                        break;
                    case 19802:
                        PutUInt32 = 0x004111B6;
                        break;
                    default:
                        PutUInt32 = 0x0;
                        break;
                }
                var subCall = (uint)(currPatternOffset + callIndex) + callOffset + 5;
                if (subCall != PutUInt32 && PutUInt32 != 0) // CDataStore::PutInt32
                    continue;

                var opcodeValue = specificOpcode == 0xBADD ? BitConverter.ToUInt16(bytes, 10) : specificOpcode;
                var ptBytes = BitConverter.GetBytes(currPatternOffset + 0x400C00);
                var vtablePattern = new byte[] {
                    0xFF, 0xFF, 0xFF, 0xFF, // Ctor
                    0xFF, 0xFF, 0xFF, 0xFF, // CliPut
                    ptBytes[0], ptBytes[1], ptBytes[2], ptBytes[3], // CliPutWithMsgId (where we are at)
                    0xFF, 0xFF, 0xFF, 0xFF  // Dtor
                };

                var vtOffsets = Program.ClientBytes.FindPattern(vtablePattern, 0xFF).Where(t => (t - 0x400C00) < Program.ClientBytes.Length);
                foreach (var vtOffset in vtOffsets)
                {
                    Program.BaseStream.Seek(vtOffset + 4, SeekOrigin.Begin);
                    var cliPut = Program.ClientStream.ReadUInt32();
                    var cliPutWithMsg = Program.ClientStream.ReadUInt32();
                    if (cliPutWithMsg != (currPatternOffset + 0x400C00))
                        continue;

                    List<uint> callerOffset;

                    if (Config.BinDiff != string.Empty)
                    {
                        var offBytes = BitConverter.GetBytes(vtOffset + 0x400C00);
                        var ctorPattern = new byte[] {
                            0x8B, 0xC1,                                                         // mov     eax, ecx
                            0x83, 0x60, 0x0C, 0x00,                                             // and     dword ptr [eax+0Ch], 0
                            0xC7, 0x00, offBytes[0], offBytes[1], offBytes[2], offBytes[3]      // mov     dword ptr [eax], <vtable>
                        };

                        callerOffset = Program.ClientBytes.FindPattern(ctorPattern, 0xFF);
                    }
                    else
                        callerOffset = new List<uint>() { 0 };

                    cmsgInfo.Add(new CMSGInfo(Opcodes.GetOpcodeNameForClient(opcodeValue,
                                callerOffset.FirstOrDefault() + 0x400C00),
                                opcodeValue,
                                vtOffset + 0x400C00,
                                cliPut,
                                cliPutWithMsg,
                                Program.FuncDiff != null ? Program.FuncDiff.getCertianty(callerOffset.FirstOrDefault() + 0x400C00) : 0));

                    Console.WriteLine(cmsgInfo.Last().getPrintString());

                    ++opcodeCount;

                    break;
                }
            }

            cmsgInfo = cmsgInfo.OrderBy(x => x.Name).ToList();
            foreach (CMSGInfo cmsg in cmsgInfo)
            {
                if (cmsg.Opcode != 0x0105) //OTHER UGLY HACK
                {
                    cmsg.FormatName();
                    Logger.WriteLine(cmsg.getPrintString());
                }
            }


            Logger.WriteLine("+---------------+------------+------------+------------+");
            Logger.WriteLine("Dumped {0} CMSG JAM opcodes.", opcodeCount);
        }

        public static void dumpWPPFile(string path)
        {
            StreamWriter output = new StreamWriter(File.Create(path));

            output.WriteLine(
                "        private static readonly BiDictionary<Opcode, int> ClientOpcodes = new BiDictionary<Opcode, int>",
                "        {");

            foreach (CMSGInfo cmsg in cmsgInfo)
            {
                if (cmsg.Name != string.Empty && (cmsg.Certianty >= 0.9f || cmsg.Certianty == 0))
                {
                    output.WriteLine(
                        "            {{Opcode.{0}, 0x{1:X4}{2}",
                        cmsg.Name,
                        cmsg.Opcode,
                        "}");
                }
            }
            output.WriteLine(
                "        };");
            output.Flush();
            output.Close();
        }
    }



    public class CMSGInfo : OpcodeInfo
    {
        public UInt32 vTable { private set; get; }
        public UInt32 cliPut { private set; get; }
        public UInt32 cliPutWithMsg { private set; get; }

        public CMSGInfo(string name, UInt32 opcode, UInt32 vTable, UInt32 cliPut, UInt32 cliPutWithMsg, double certianty = 0.0f) : base(name, opcode, certianty)
        {
            this.vTable = vTable;
            this.cliPut = cliPut;
            this.cliPutWithMsg = cliPutWithMsg;
        }

        public override void FormatName()
        {
            if (Name == null)
                Name = string.Empty;

            if (!Name.StartsWith("CMSG") && Name != string.Empty)
            {
                string[] prefixes = {
                "PlayerCli",
                "Player",
                "UserClient",
                "CliChat",
                "UserRouterClient",
                "Global"
                                };

                string[] capitalized = {
                                           "PVP",
                                           "DF",
                                           "GM",
                                           "SoR",
                                           "ID",
                                           "LF"
                                       };

                StringBuilder nameBuilder = new StringBuilder(Name);

                foreach (string prefix in prefixes)
                {
                    if (Name.StartsWith(prefix))
                    {
                        nameBuilder.Replace(prefix, "");
                        break;
                    }
                }

                Name = nameBuilder.ToString();
                nameBuilder.Clear();
                nameBuilder.Append("CMSG");

                for (int i = 0; i < Name.Length; i++) // Removes 'Client' prefix from name
                {
                    if (Char.IsUpper(Name[i]))
                    {
                        bool a = true;
                        foreach (string str in capitalized)
                        {
                            if (i + str.Length <= Name.Length)
                            {
                                if (Name.Substring(i, str.Length) == str)
                                {
                                    nameBuilder.AppendFormat("_{0}", str);
                                    i += str.Length - 1;
                                    a = false;
                                    break;
                                }
                            }
                        }
                        if (a)
                            nameBuilder.AppendFormat("_{0}", Name[i]);
                    }
                    else
                        nameBuilder.Append(char.ToUpper(Name[i]));
                }

                Name = nameBuilder.ToString().ToUpper();
            }
            else
                Certianty = 0; //HORRIBLE UGLY HACK
        }
        public override string getPrintString()
        {
            return string.Format("| {0} (0x{1:X4}) | 0x{2:X8} | 0x{3:X8} |   {4:F4}   | {5}",
              Opcode.ToString().PadLeft(4),
              Opcode,
              vTable,
              cliPut,
              Certianty,
              Name);
        }
    }
}
