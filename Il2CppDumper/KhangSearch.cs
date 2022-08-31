using System;
using System.IO;

namespace Il2CppDumper {
    public static class KhangSearch {
        private static ulong ToULong(byte[] a, int start) {
            return (ulong)(a[start + 0] | (a[start + 1]<<8) | (a[start+2]<<16) | (a[start+3]<<24));
        }

        private static bool CheckPattern(byte[] bytes, int i) {
            return bytes[i +  0] == 0x4C && bytes[i +  1] == 0x8D && bytes[i +  2] == 0x05 &&
                   bytes[i +  7] == 0x48 && bytes[i +  8] == 0x8D && bytes[i +  9] == 0x15 &&
                   bytes[i + 14] == 0x48 && bytes[i + 15] == 0x8D && bytes[i + 16] == 0x0D &&
                   bytes[i + 21] == 0xE9;
        }

        private static bool CheckPatternWithUsages(byte[] bytes, int i) {
            return bytes[i + 0] == 0x4C && bytes[i + 1] == 0x8D && bytes[i + 2] == 0x0D &&
                   CheckPattern(bytes, i + 7);
        }

        public static bool SearchRegistrations(string filename, ulong imageBase, out ulong codeRegistration, out ulong metadataRegistration, out ulong usagesRegistration) {
            codeRegistration = 0;
            metadataRegistration = 0;
            usagesRegistration = 0;
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

            bool found = false;

            // functions are always aligned to 16 bytes
            const int patternLength = 29;
            for (int attempt = 0; !found && attempt < 2; attempt++) {
                // On 0th attempt, try to find all the info (with usages)
                // If it didn't succeed, skip usages
                Func<byte[], int, bool> checker = attempt == 0 ? (Func<byte[], int, bool>)CheckPatternWithUsages : CheckPattern; // Fuck C#

                for (int i = 0; !found && i < bytes.Length - patternLength; i += 0x1)
                {
                    if (checker(bytes, i))
                    {
                        codeRegistration = (ulong)i + 28 + ToULong(bytes, i + 21 + 3);
                        metadataRegistration = (ulong)i + 21 + BitConverter.ToUInt32(bytes, i + 14 + 3);
                        if (attempt == 0)
                            usagesRegistration = (ulong)i + 14 + ToULong(bytes, i + 7 + 3);
                        Console.WriteLine($"Found the offsets! codeRegistration: 0x{(codeRegistration).ToString("X2")}, metadataRegistration: 0x{(metadataRegistration).ToString("X2")}, usagesRegistration: 0x{(usagesRegistration).ToString("X2")}");
                        found = true;
                    }
                }
            }

            if (codeRegistration == 0 && metadataRegistration == 0)
            {
                Console.WriteLine("Failed to find CodeRegistration, MetadataRegistration and UsagesRegistration, go yell at Khang and curse nitro");
                return false;
            }

            if (usagesRegistration == 0)
            {
                Console.WriteLine("Failed to find UsagesRegistration. If you provided an old version of the data, it's fine; otherwise parts of the output might be nonsense.");
            }

            ulong bas = imageBase + 3072;

            codeRegistration += bas;
            metadataRegistration += bas;

            if (usagesRegistration != 0)
                usagesRegistration += bas;

            return true;
        }

        #if false
        public static void Main(string [] args) {
            Console.WriteLine("Hello, World!");
            ulong codeRegistration = 0;
            ulong metadataRegistration = 0;
            ulong usagesRegistration = 0;
            if (SearchRegistrations(args[0], 0x180000000, out codeRegistration, out metadataRegistration, out usagesRegistration)) {
                Console.WriteLine("0x{0:X}, 0x{1:X}, 0x{2:X}", codeRegistration, metadataRegistration, usagesRegistration);
            }
        }
        #endif
    }
}
