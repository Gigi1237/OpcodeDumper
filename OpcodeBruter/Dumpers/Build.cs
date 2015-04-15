using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcodeBruter.Dumpers
{
    public class Build
    {
        // TODO: Use better method for detecting build
        private static byte[] Pattern = new byte[] {
                0x8B, 0x75, 0x08,               // mov     esi, [ebp+arg_0]
                0x54,                           // push    esp
                0x56,                           // push    esi
                0xE8, 0xFF, 0xFF, 0xFF, 0xFF,   // call    lua_pushstring
                0x68, 0xFF, 0xFF, 0xFF, 0x00,   // push    offset <Version>
                0x56,                           // push    esi
                0xE8, 0xFF, 0xFF, 0xFF, 0xFF,   // call    lua_pushstring
                0x68, 0xFF, 0xFF, 0xFF, 0x00,   // push    offset <BuildNumber>
                0x56,                           // push    esi
                0xE8, 0xFF, 0xFF, 0xFF, 0xFF,   // call    lua_pushstring
            };
        private static Dictionary<int, bool> SupportedBuilds = new Dictionary<int, bool>()
        {
            { 19342, true },
            { 19702, true },
            { 19802, true },
            { 19865, true }
        };
        public string Version { private set; get; }
        public int BuildNumber { private set; get; }

        public Build()
        {
            var patternOffsets = Program.ClientBytes.FindPattern(Pattern, 0xFF);
            Program.BaseStream.Seek(patternOffsets[0], SeekOrigin.Begin);

            var bytes = Program.ClientStream.ReadBytes(Pattern.Length);

            var VersionOffset = BitConverter.ToInt32(bytes, 11);
            var BuildOffset = BitConverter.ToInt32(bytes, 22);

            Program.BaseStream.Seek(VersionOffset - 0x400E00, SeekOrigin.Begin);
            Version = new String(Program.ClientStream.ReadChars(5));

            Program.BaseStream.Seek(BuildOffset - 0x400E00, SeekOrigin.Begin);
            BuildNumber = Int32.Parse(new String(Program.ClientStream.ReadChars(5)));
        }

        public bool isBuildSupported()
        {
            if (BuildNumber != 0)
            {
                bool isSupported = false;
                if (SupportedBuilds.TryGetValue(BuildNumber, out isSupported))
                    return isSupported;
                else
                    return false;
            }
            else
                return false;
        }
    }
}
