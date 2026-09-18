using CmlLib.Core;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace TwistClient.Services
{
    public class MinecraftService
    {
        public MinecraftPath MinecraftPath { get; }

        public MinecraftService()
        {
            MinecraftPath = new MinecraftPath();
        }

        public MinecraftLauncher CreateLauncher()
        {
            return new MinecraftLauncher(MinecraftPath);
        }

        public async Task InstallVersionAsync(string version)
        {
            var launcher = CreateLauncher();
            await launcher.InstallAsync(version);
        }

        public bool IsVersionInstalled(string version)
        {
            string jarPath = Path.Combine(
                MinecraftPath.BasePath,
                "versions",
                version,
                $"{version}.jar"
            );

            return File.Exists(jarPath);
        }

        // 🛠️ THE LUNAR KILLER: Automated Parallel Mod Downloader Component
        // Streams optimization binaries directly from network servers straight into the player's active directory
        public async Task DownloadModAsync(string modName, string downloadUrl)
        {
            try
            {
                string modsFolder = Path.Combine(MinecraftPath.BasePath, "mods");
                Directory.CreateDirectory(modsFolder); // Ensure the local directory path exists

                string targetJarPath = Path.Combine(modsFolder, $"{modName}.jar");

                // Optimization Shield: If the mod binary is already sitting on the hard drive, skip downloading to boot instantly!
                if (File.Exists(targetJarPath)) return;

                using var httpClient = new HttpClient();

                // Fetch the binary stream from remote repo nodes
                byte[] fileBytes = await httpClient.GetByteArrayAsync(downloadUrl);

                // Write the raw jar file safely onto the local operating system path
                await File.WriteAllBytesAsync(targetJarPath, fileBytes);

                System.Diagnostics.Debug.WriteLine($"Twist Optimization Mod Injected Successfully: {modName}");
            }
            catch (Exception ex)
            {
                // Non-breaking fallback tracking log loop
                System.Diagnostics.Debug.WriteLine($"Mod streaming bypass vector exception: {ex.Message}");
            }
        }
    }
}
