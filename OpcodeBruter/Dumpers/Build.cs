using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace OpcodeBruter.Dumpers
{
    public enum BuildSupport
    {
        BUILD_SUPPORTED,
        BUILD_UNSUPPORTED,
        BUILD_UNKOWN
    };

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

        // Change to false if offsets are changed
        private static Dictionary<int, bool> SupportedBuilds = new Dictionary<int, bool>()
        {
            { 19342, false },
            { 19702, false },
            { 19802, false },
            { 19865, true }
        };
        public string Version { private set; get; }
        public int BuildNumber { private set; get; }

        public Build()
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(Config.Executable);
            Version = String.Format("{0}.{1}.{2}", versionInfo.FileMajorPart, versionInfo.FileBuildPart, versionInfo.FileMinorPart);
            BuildNumber = versionInfo.ProductPrivatePart;
        }

        public BuildSupport isBuildSupported()
        {
            if (BuildNumber != 0)
            {
                bool isSupported = false;
                if (SupportedBuilds.TryGetValue(BuildNumber, out isSupported))
                    return isSupported ? BuildSupport.BUILD_SUPPORTED : BuildSupport.BUILD_UNSUPPORTED;
            }

            return BuildSupport.BUILD_UNKOWN;
        }
    }
}
