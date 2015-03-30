using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using x86;

namespace OpcodeBruter
{
    class Program
    {
        public static List<JamDispatch> Dispatchers = new List<JamDispatch>();
        public static Dictionary<uint, uint> OpcodeToFileOffset = new Dictionary<uint, uint>();

        public static Emulator Env { get; private set; }
        public static BinaryReader ClientStream { get; private set; }
        public static Stream BaseStream { get { return ClientStream.BaseStream; } }
        public static byte[] ClientBytes { get; private set; }
        public static UnmanagedBuffer Disasm { get; private set; }

        // Used to populate Dispatchers.
        // First byte is used to avoid multiple matches on Client - tons of false positives there.
        //! DOUBLECHECK THESE FOR ANY BUILD
        public static byte[][] ClientGroupPatterns = new byte[][] {
            new byte[] { 0x00, 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x00 }, // Client
            new byte[] { 0x00, 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x43, 0x68, 0x61, 0x74, 0x00 }, // ClientChat
            new byte[] { 0x00, 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x47, 0x61, 0x72, 0x72, 0x69, 0x73, 0x6F, 0x6E, 0x00 }, // ClientGarrison
            new byte[] { 0x00, 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x47, 0x75, 0x69, 0x6C, 0x64, 0x00 }, // ClientGuild
            new byte[] { 0x00, 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x4C, 0x46, 0x47, 0x00 }, // ClientLFG
            new byte[] { 0x00, 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x4D, 0x6F, 0x76, 0x65, 0x6D, 0x65, 0x6E, 0x74, 0x00 }, // ClientMovement
            new byte[] { 0x00, 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x51, 0x75, 0x65, 0x73, 0x74, 0x00 }, // ClientQuest
            new byte[] { 0x40, 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x53, 0x6F, 0x63, 0x69, 0x61, 0x6C, 0x00 }, // ClientSocial
            new byte[] { 0x41, 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x53, 0x70, 0x65, 0x6C, 0x6C, 0x00 } // ClientSpell
        };

        static void Main(string[] args)
        {
            Console.WriteLine(">> Loading configuration options...");
            if (!Config.Load(args))
                return;

            if (Config.NoCmsg && Config.NoSmsg)
            {
                Logger.WriteConsoleLine("Please give me something to do.");
                Config.ShowHelp();
                return;
            }

            Logger.CreateOutputStream(Config.OutputFile);

            Console.WriteLine(">> Opening Wow client...");
            ClientStream = new BinaryReader(File.OpenRead(Config.Executable));
            if (!BaseStream.CanRead)
                return;

            ClientBytes = File.ReadAllBytes(Config.Executable);
            Disasm = new UnmanagedBuffer(ClientBytes);
            Env = Emulator.Create(BaseStream);

            if (!Config.NoSmsg)
            {
                Console.WriteLine(">> Discovering JAM groups...");
                foreach (var pattern in ClientGroupPatterns) // Load jam groups...
                {
                    var offsets = ClientBytes.FindPattern(pattern, 0xFF);
                    if (offsets.Count == 0)
                    {
                        Console.WriteLine(@"Could not find group name "" {0}""", System.Text.Encoding.ASCII.GetString(pattern.Skip(1).ToArray()));
                        return;
                    }
                    else
                    {
                        Console.WriteLine(@"Found JAM Group "" {0}""", System.Text.Encoding.ASCII.GetString(pattern.Skip(1).ToArray()));
                        Dispatchers.Add(new JamDispatch((int)(offsets[0] + 1)));
                    }
                }
            }

            if (!Config.NoGhNames && !Opcodes.TryPopulate())
                return;

            if (!Config.NoSmsg)
                SMSG.Dump();

            if (!Config.NoCmsg)
                CMSG.Dump(Config.SpecificOpcodeValue);
        }
    }
}
