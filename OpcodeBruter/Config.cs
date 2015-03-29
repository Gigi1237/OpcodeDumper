using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace OpcodeBruter
{
    /// <summary>
    /// Parses command line arguments.
    /// </summary>
    public static class Config
    {
        [ConfigKey("-op", "Defines a specific opcode value to use.")]
        public static uint SpecificOpcodeValue = 0xBADD;

        [ConfigKey("-debug", "Controls for extended debug info to be written to the console.")]
        public static bool Debug = false;

        [ConfigKey("-no-gh-names", "Controls if names from WPP are to be downloaded.")]
        public static bool NoGhNames = false;
        
        [ConfigKey("-no-client", "Controls if CMSG opcodes are to be dumped.")]
        public static bool NoCmsg = true;

        [ConfigKey("-no-server", "Controls if SMSG opcodes are to be dumped.")]
        public static bool NoSmsg = true;

        [ConfigKey("-e", "Path to the wow executable.")]
        public static string Executable = AppDomain.CurrentDomain.BaseDirectory + "./Wow.exe";

        [ConfigKey("-of", "Path to the file used for output.")]
        public static string OutputFile = String.Empty;

        [ConfigKey("-wpp", "Dump opcodes to WPP compatible array. (CMSG only)")]
        public static bool WPP = false;

        [ConfigKey("-d", "Path to bindiff results. Requires -o flag. (CMSG only)")]
        public static string BinDiff = String.Empty;

        [ConfigKey("-o", "Path to 6.0.3 sqlite opcode file. Requires -d flag. (CMSG only)")]
        public static string Opcode = String.Empty;

        [ConfigKey("-help", "Shows this help.")]
        public static bool help = false;

        public static bool Load(string[] args)
        {
            var showHelp = !TryGet<uint>(args, "-op", ref SpecificOpcodeValue, 0xBADD);
            showHelp = !TryGet<bool>(args, "-debug", ref Debug, false);
            showHelp = !TryGet<bool>(args, "-no-client", ref NoCmsg, false);
            showHelp = !TryGet<bool>(args, "-no-server", ref NoSmsg, false);
            showHelp = !TryGet<bool>(args, "-no-gh-names", ref NoGhNames, false);
            showHelp = !TryGet<bool>(args, "-wpp", ref WPP, false);
            showHelp = !TryGet<string>(args, "-of", ref OutputFile, String.Empty);
            showHelp = !TryGet<string>(args, "-d", ref BinDiff, String.Empty);
            showHelp = !TryGet<string>(args, "-o", ref Opcode, String.Empty);
            showHelp = !TryGet<string>(args, "-e", ref Executable, AppDomain.CurrentDomain.BaseDirectory + "./Wow.exe");
            TryGet<bool>(args, "-help", ref help, false);

            return showHelp || help ? ShowHelp() : true;
        }

        public static bool ShowHelp()
        {
            Console.WriteLine("Arguments:");

            var fields = typeof(Config).GetFields(BindingFlags.Static | BindingFlags.Public);
            var keys = new List<string>();
            var desc = new List<string>();

            foreach (var f in typeof(Config).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                ConfigKey attr = null; 
                if ((attr = (ConfigKey)f.GetCustomAttribute(typeof(ConfigKey), false)) != null)
                {
                    keys.Add(attr.Key);
                    desc.Add(attr.Description);
                }
            }
            
            var padLength = keys.Max(s => s.Length);
            for (var i = 0; i < keys.Count; ++i)
                Logger.WriteConsoleLine(" {0} : {1}", keys[i].PadRight(padLength), desc[i]);
            return false;
        }

        public static bool TryGet<T>(string[] args, string argName, ref T dest, T defaultValue)
        {
            var keyType = Type.GetTypeCode(typeof(T));

            var index = Array.IndexOf(args, argName);
            if (keyType == TypeCode.Boolean || index == -1)
            {
                if (index == -1)
                    dest = defaultValue;
                else if (keyType == TypeCode.Boolean)
                    using (var box = new ValueBox<T>())
                    {
                        box.ObjectValue = index != -1;
                        dest = box.GetValue();
                    }
                return false;
            }
            else
            {
                var val = args[index + 1];
                using (var box = new ValueBox<T>())
                {
                    switch (keyType)
                    {
                        case TypeCode.UInt32:
                            box.ObjectValue = Convert.ToUInt32(val);
                            break;
                        case TypeCode.Int32:
                            box.ObjectValue = Convert.ToInt32(val);
                            break;
                        case TypeCode.UInt16:
                            box.ObjectValue = Convert.ToUInt16(val);
                            break;
                        case TypeCode.Int16:
                            box.ObjectValue = Convert.ToInt16(val);
                            break;
                        case TypeCode.Byte:
                            box.ObjectValue = Convert.ToByte(val);
                            break;
                        case TypeCode.SByte:
                            box.ObjectValue = Convert.ToSByte(val);
                            break;
                        case TypeCode.Single:
                            box.ObjectValue = Convert.ToSingle(val);
                            break;
                        case TypeCode.String:
                            box.ObjectValue = Convert.ToString(val);
                            break;
                        default:
                            Logger.WriteConsoleLine("Unable to read value for argument {0}", argName);
                            break;
                    }
                    dest = box.GetValue();
                }
                return true;
            }
        }
    }

    public sealed class ValueBox<T> : IDisposable
    {
        public object ObjectValue;

        public T GetValue()
        {
            return (T)ObjectValue;
        }

        public void Dispose() { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigKey : Attribute
    {
        public string Description;
        public string Key;

        public ConfigKey(string key, string attr, params object[] obj)
        {
            Key = key;
            Description = String.Format(attr, obj);
        }
    }
}
