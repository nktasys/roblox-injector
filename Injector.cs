using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MizaKusense
{
    public static class Injector
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        public static void Inject(Dictionary<string, JsonElement> flags, Dictionary<string, int> offsets, bool safeMode)
        {
            var processes = Process.GetProcessesByName("RobloxPlayerBeta");
            if (processes.Length == 0)
            {
                throw new Exception("Roblox process not found!");
            }

            var process = processes[0];
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

            if (hProcess == IntPtr.Zero)
            {
                throw new Exception("Failed to open Roblox process!");
            }

            try
            {
                IntPtr baseAddress = process.MainModule.BaseAddress;
                long baseVal = baseAddress.ToInt64();

                int successCount = 0;
                int failCount = 0;

                foreach (var flag in flags)
                {
                    string shortName = StripPrefix(flag.Key);
                    
                    if (!offsets.TryGetValue(shortName, out int offset))
                    {
                        continue;
                    }

                    long addr = baseVal + offset;
                    IntPtr address = new IntPtr(addr);

                    byte[] data = EncodeFlagValue(flag.Key, flag.Value);
                    if (data != null)
                    {
                        IntPtr bytesWritten;
                        if (WriteProcessMemory(hProcess, address, data, data.Length, out bytesWritten))
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                }

                if (successCount == 0 && failCount == 0)
                {
                    throw new Exception("No flags were injected. Check if offsets match the loaded flags.");
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        private static string StripPrefix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string[] prefixes = { "FFlag", "DFFlag", "FInt", "DFInt", "FLog", "DFLog", "FString", "DFString" };

            foreach (string p in prefixes)
            {
                if (name.StartsWith(p, StringComparison.Ordinal))
                    return name.Substring(p.Length);
            }

            return name;
        }

        private static byte[] EncodeFlagValue(string fullName, JsonElement el)
        {
            bool isBoolFlag = fullName.StartsWith("FFlag", StringComparison.Ordinal) ||
                              fullName.StartsWith("DFFlag", StringComparison.Ordinal);

            bool isIntFlag = fullName.StartsWith("FInt", StringComparison.Ordinal) ||
                             fullName.StartsWith("DFInt", StringComparison.Ordinal);

            if (isBoolFlag)
            {
                bool value;
                if (el.ValueKind == JsonValueKind.True) value = true;
                else if (el.ValueKind == JsonValueKind.False) value = false;
                else return null;

                return new[] { value ? (byte)1 : (byte)0 };
            }

            if (isIntFlag)
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int value))
                {
                    return BitConverter.GetBytes(value);
                }
            }

            return null;
        }
    }
}
