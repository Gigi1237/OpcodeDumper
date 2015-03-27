using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpcodeBruter
{
    public static class Opcodes
    {

        public static Regex OpcodeRgx = new Regex(@"Opcode\.(.+), 0x([A-Z0-9]+)(?: | 0x[0-9A-Z]+)?", RegexOptions.IgnoreCase);

        private static string FilePath = @"https://raw.githubusercontent.com/TrinityCore/WowPacketParser/master/WowPacketParser/Enums/Version/V6_1_2_19802/Opcodes.cs";
        public static bool TryPopulate(bool smsg = true)
        {
            if ((smsg ? SMSG : CMSG).Count != 0)
                return true;

            Logger.WriteConsoleLine("Loading opcodes from GitHub, build 19802...");
            try
            {
                WebClient client = new WebClient();
                Stream stream = client.OpenRead(FilePath);
                StreamReader reader = new StreamReader(stream);
                var content = reader.ReadToEnd().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                stream.Close();
                foreach (var line in content)
                {
                    var rgxResult = OpcodeRgx.Match(line);
                    if (!rgxResult.Success)
                        continue;

                    var opcodeName = rgxResult.Groups[1].Value;
                    var opcodeValue = Convert.ToInt32(rgxResult.Groups[2].Value, 16);
                    if (opcodeName.Contains("CMSG"))
                        CMSG.Add(opcodeName, opcodeValue);
                    else
                        SMSG.Add(opcodeName, opcodeValue);
                }
            }
            catch (WebException /*whatever*/) // Haha so funny I is.
            {
                Logger.WriteConsoleLine("Unable to query opcodes. Exiting. Try again.");
                return false;
            }

            return (smsg ? SMSG : CMSG).Count != 0;
        }

        public static string GetOpcodeNameForServer(uint opcode, uint Handler)
        {
            string output = SMSG.FirstOrDefault(p => p.Value == opcode).Key;
            //if (Config.BinDiff != string.Empty && output == null)
            //    output = Program.OpTable.getSMSGNameFromHandler(Program.FuncDiff.getOldFunction(Handler)); // SMSG diff is not working yet

            return output;
        }

        public static string GetOpcodeNameForClient(uint opcode, uint Ctor)
        {
            string output = CMSG.FirstOrDefault(p => p.Value == opcode).Key;
            if (Config.BinDiff != string.Empty && output == null)
                output = Program.OpTable.getCMSGNameFromCtor(Program.FuncDiff.getOldFunction(Ctor));

            return output;

        }

        public static Dictionary<string, int> CMSG = new Dictionary<string, int>();
        public static Dictionary<string, int> SMSG = new Dictionary<string, int>();
    }
}
