using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace UltrakillCheat
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        static IntPtr healthAddr = IntPtr.Zero;
        static IntPtr staminaAddr = IntPtr.Zero;
        static IntPtr ammoAddr = IntPtr.Zero;
        
        static int newHealth = 9999;
        static float newStamina = 300f;
        static float newAmmo = 100f;
        
        static bool freezeAll = false;

        static void Main(string[] args)
        {
            Console.Title = "ULTRAKILL External Cheat - By [Nama Kamu]";
            Console.WriteLine("=========================================");
            Console.WriteLine("   ULTRAKILL External Cheat (Tugas Kuliah)");
            Console.WriteLine("=========================================");

            Process[] processes = Process.GetProcessesByName("ULTRAKILL");
            if (processes.Length == 0)
            {
                Console.WriteLine("[!] Game ULTRAKILL tidak berjalan. Jalankan game terlebih dahulu.");
                Console.ReadKey(); return;
            }

            Process game = processes[0];
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, game.Id);
            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine("[!] Gagal membuka proses. Jalankan aplikasi ini sebagai Administrator.");
                Console.ReadKey(); return;
            }

            IntPtr unityBase = GetModuleBaseAddress(game, "UnityPlayer.dll");
            IntPtr monoBase = GetModuleBaseAddress(game, "mono-2.0-bdwgc.dll");

            if (unityBase == IntPtr.Zero || monoBase == IntPtr.Zero)
            {
                Console.WriteLine("[!] Gagal menemukan DLL game. Pastikan sudah masuk ke dalam level.");
                CloseHandle(hProcess); Console.ReadKey(); return;
            }

            healthAddr = FindDllPointer(hProcess, unityBase, 0x017B6DF8, new int[] { 0xA0, 0x78, 0x18, 0x48, 0x130, 0x28, 0x20C });
            
            staminaAddr = FindDllPointer(hProcess, unityBase, 0x017B6E40, new int[] { 0x30, 0x30, 0x18, 0x140, 0xB8, 0x60, 0x288 });
            
            ammoAddr = FindDllPointer(hProcess, monoBase, 0x00494DE8, new int[] { 0x60, 0x9D0, 0x78, 0x20, 0x28, 0x1C8, 0x4F0 });

            if (healthAddr == IntPtr.Zero || staminaAddr == IntPtr.Zero || ammoAddr == IntPtr.Zero)
            {
                Console.WriteLine("[!] Gagal menelusuri pointer chain. Offset mungkin berubah atau belum masuk level.");
                CloseHandle(hProcess); Console.ReadKey(); return;
            }

            Console.WriteLine("[+] Semua alamat berhasil ditemukan!");
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine($"Health  : {ReadInt32(hProcess, healthAddr)}");
            Console.WriteLine($"Stamina : {ReadFloat(hProcess, staminaAddr)}");
            Console.WriteLine($"Ammo    : {ReadFloat(hProcess, ammoAddr)}");
            Console.WriteLine("-----------------------------------------");

            Console.Write($"Masukkan Health baru (default {newHealth}): ");
            if (int.TryParse(Console.ReadLine(), out int h)) newHealth = h;

            Console.Write($"Masukkan Stamina baru (default {newStamina}): ");
            if (float.TryParse(Console.ReadLine(), out float s)) newStamina = s;

            Console.Write($"Masukkan Ammo baru (default {newAmmo}): ");
            if (float.TryParse(Console.ReadLine(), out float a)) newAmmo = a;

            WriteInt32(hProcess, healthAddr, newHealth);
            WriteFloat(hProcess, staminaAddr, newStamina);
            WriteFloat(hProcess, ammoAddr, newAmmo);

            Console.WriteLine("\n[+] Nilai berhasil diubah!");
            Console.WriteLine("Tekan 'F' untuk mengunci (Freeze) semua nilai agar tidak berkurang.");
            Console.WriteLine("Tekan 'Q' untuk keluar.");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.F)
                    {
                        freezeAll = !freezeAll;
                        Console.WriteLine(freezeAll ? "\n[FREEZE AKTIF]" : "\n[FREEZE NONAKTIF]");
                    }
                    else if (key == ConsoleKey.Q) break;
                }

                if (freezeAll)
                {
                    WriteInt32(hProcess, healthAddr, newHealth);
                    WriteFloat(hProcess, staminaAddr, newStamina);
                    WriteFloat(hProcess, ammoAddr, newAmmo);
                }

                Thread.Sleep(50); 
            }

            CloseHandle(hProcess);
            Console.WriteLine("Cheat ditutup.");
        }

        static IntPtr GetModuleBaseAddress(Process process, string moduleName)
        {
            foreach (ProcessModule module in process.Modules)
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                    return module.BaseAddress;
            return IntPtr.Zero;
        }

        static IntPtr FindDllPointer(IntPtr hProcess, IntPtr moduleBase, int baseOffset, int[] offsets)
        {
            IntPtr address = IntPtr.Add(moduleBase, baseOffset);
            byte[] buffer = new byte[8];

            foreach (int offset in offsets)
            {
                if (!ReadProcessMemory(hProcess, address, buffer, buffer.Length, out _)) return IntPtr.Zero;
                long pointerValue = BitConverter.ToInt64(buffer, 0);
                if (pointerValue == 0) return IntPtr.Zero;
                address = IntPtr.Add((IntPtr)pointerValue, offset);
            }
            return address;
        }

        static int ReadInt32(IntPtr hProcess, IntPtr address)
        {
            byte[] buffer = new byte[4];
            ReadProcessMemory(hProcess, address, buffer, buffer.Length, out _);
            return BitConverter.ToInt32(buffer, 0);
        }

        static void WriteInt32(IntPtr hProcess, IntPtr address, int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteProcessMemory(hProcess, address, buffer, buffer.Length, out _);
        }

        static float ReadFloat(IntPtr hProcess, IntPtr address)
        {
            byte[] buffer = new byte[4];
            ReadProcessMemory(hProcess, address, buffer, buffer.Length, out _);
            return BitConverter.ToSingle(buffer, 0);
        }

        static void WriteFloat(IntPtr hProcess, IntPtr address, float value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteProcessMemory(hProcess, address, buffer, buffer.Length, out _);
        }
    }
}