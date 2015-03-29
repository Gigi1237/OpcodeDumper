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
        public static Dictionary<JamGroup, JamDispatch> Dispatchers = new Dictionary<JamGroup, JamDispatch>();
        public static Dictionary<uint, uint> OpcodeToFileOffset = new Dictionary<uint, uint>();

        public static Emulator Environment { get; private set; }
        public static BinaryReader ClientStream { get; private set; }
        public static Stream BaseStream { get { return ClientStream.BaseStream; } }
        public static byte[] ClientBytes { get; private set; }
        public static UnmanagedBuffer Disasm { get; private set; }
        public static BinDiff FuncDiff { get; private set; }
        public static OpcodeTable OpTable { get; private set; }
        public static Dumpers.Build ClientBuild { get; private set; }

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
            try
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

                if ((Config.BinDiff == string.Empty) != (Config.Opcode == string.Empty))
                {
                    Console.WriteLine("ERROR: -d and -o flags need to be both present to function!");
                    return;
                }

                if (Config.BinDiff != string.Empty && Config.Opcode != string.Empty)
                {
                    Console.WriteLine(">> Opening Diff...");
                    FuncDiff = new BinDiff(Config.BinDiff);
                    if (!FuncDiff.openConnection())
                    {
                        Console.WriteLine(">> Failed to open diff!");
                        return;
                    }

                    Console.WriteLine(">> Opening OpcodeTable...");
                    OpTable = new OpcodeTable(Config.Opcode);
                    if (!OpTable.openConnection())
                    {
                        Console.WriteLine(">> Failed to open OpcodeTable!");
                        return;
                    }
                }

                if (Logger.CreateOutputStream(Config.OutputFile))
                    Logger.PrepOutputStram();

                Console.WriteLine(">> Opening Wow client...");
                ClientStream = new BinaryReader(File.OpenRead(Config.Executable));
                if (!BaseStream.CanRead)
                    return;

                ClientBytes = File.ReadAllBytes(Config.Executable);
                Disasm = new UnmanagedBuffer(ClientBytes);
                Environment = Emulator.Create(BaseStream);

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
                        var dispatch = new JamDispatch((int)(offsets[0] + 1));
                        Dispatchers[dispatch.GetGroup()] = dispatch;
                    }
                }

                ClientBuild = new Dumpers.Build();

                if (ClientBuild.isBuildSupported())
                    Logger.WriteLine("Detected build {0}.{1}", ClientBuild.Version, ClientBuild.BuildNumber);
                else
                {
                    Console.WriteLine("ERROR! Build unsupported!");
                    return;
                }

                if (!Config.NoGhNames && !Opcodes.TryPopulate())
                    return;

                if (!Config.NoSmsg)
                    Dumpers.SMSG.Dump();

                if (!Config.NoCmsg)
                    Dumpers.CMSG.Dump(Config.SpecificOpcodeValue);

                if (Config.WPP)
                    Dumpers.CMSG.dumpWPPFile("Opcodes.cs");
            }
            catch(SystemException e)
            {
                Console.WriteLine("Caught level exception: \n{0}\n\n Did you use the correct command line arguments?use -help to show usage.\nPress any key to exit...", e.Message);
                Console.ReadKey();
            }
        }
    }
}
