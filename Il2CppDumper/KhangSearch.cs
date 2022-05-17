using System;
using System.IO;

namespace Il2CppDumper {
    public static class KhangSearch {
        private static ulong ToULong(byte[] a, int start) {
            return (ulong)(a[start + 0] | (a[start + 1]<<8) | (a[start+2]<<16) | (a[start+3]<<24));
        }

        public static bool SearchRegistrations(string filename, out ulong codeRegistration, out ulong metadataRegistration) {
            codeRegistration = 0;
            metadataRegistration = 0;
            // custom search
            // searching .text for the following pattern:
            // lea r8,  [rip+0x????????]
            // lea rdx, [rip+0x????????]
            // lea rcx, [rip+0x????????]
            // jmp [rip+0x????????]
            // or...
            // 4c 8d 05 ?? ?? ?? ??
            // 48 8d 15 ?? ?? ?? ??
            // 48 8d 0d ?? ?? ?? ??
            // e9
            // 22 bytes long

            var bytes = File.ReadAllBytes(filename);

            // functions are always aligned to 16 bytes
            const int patternLength = 22;
            for (int i = 0; i < bytes.Length - patternLength; i += 0x1)
            {
                if (
                    bytes[i +  0] == 0x4C && bytes[i +  1] == 0x8D && bytes[i +  2] == 0x05 &&
                    bytes[i +  7] == 0x48 && bytes[i +  8] == 0x8D && bytes[i +  9] == 0x15 &&
                    bytes[i + 14] == 0x48 && bytes[i + 15] == 0x8D && bytes[i + 16] == 0x0D &&
                    bytes[i + 21] == 0xE9
                )
                {
                    codeRegistration = (ulong)i + 21 + ToULong(bytes, i + 14 + 3);
                    metadataRegistration = (ulong)i + 14 + BitConverter.ToUInt32(bytes, i + 7 + 3);
                    Console.WriteLine($"Found the offsets! codeRegistration: 0x{(codeRegistration).ToString("X2")}, metadataRegistration: 0x{(metadataRegistration).ToString("X2")}");
                    break;
                }
            }

            if (codeRegistration == 0 && metadataRegistration == 0)
            {
                Console.WriteLine("Failed to find CodeRegistration and MetadataRegistration, go yell at Khang");
                return false;
            }

            ulong bas = 0x180000000 + 3072;

            codeRegistration += bas;
            metadataRegistration += bas;

            return true;
        }

        /*public static void Main(string [] args) {
            Console.WriteLine("Hello, World!");
            ulong codeRegistration = 0;
            ulong metadataRegistration = 0;
            if (SearchRegistrations(args[0], out codeRegistration, out metadataRegistration)) {
                ulong bas = 0x180000000 + 3072;
                Console.WriteLine("0x{0:X}, 0x{1:X}", codeRegistration + bas, metadataRegistration + bas);
            }
        }*/
    }
}
