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

        // 🛠️ THE LUNAR KILLER: Automated High-Speed Mod Downloader
        // Downloads any mod from a URL and injects it straight into AppData/.minecraft/mods
        public async Task DownloadModAsync(string modName, string downloadUrl)
        {
            try
            {
                string modsFolder = Path.Combine(MinecraftPath.BasePath, "mods");
                Directory.CreateDirectory(modsFolder); // Automatically build the folder if it doesn't exist

                string targetJarPath = Path.Combine(modsFolder, $"{modName}.jar");

                // Smart Check: If the mod is already downloaded, skip it to launch instantly!
                if (File.Exists(targetJarPath)) return;

                using var httpClient = new HttpClient();

                // Fetch the mod file from the remote download link
                byte[] fileBytes = await httpClient.GetByteArrayAsync(downloadUrl);

                // Write it safely to the mods folder
                await File.WriteAllBytesAsync(targetJarPath, fileBytes);

                System.Diagnostics.Debug.WriteLine($"[TwistEngine] Mod injected successfully: {modName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TwistEngine] Mod download failed: {ex.Message}");
            }
        }
    }
}
