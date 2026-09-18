using CmlLib.Core;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TwistClient.Services
{
    public class MinecraftService
    {
        public MinecraftPath MinecraftPath { get; }

        public MinecraftService()
        {
            // Standardize files directly inside a custom sandbox to isolate them from TLauncher entries
            string customPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".twistclient"
            );
            MinecraftPath = new MinecraftPath(customPath);
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

        public async Task DownloadModAsync(string modName, string downloadUrl)
        {
            try
            {
                string modsFolder = Path.Combine(MinecraftPath.BasePath, "mods");
                Directory.CreateDirectory(modsFolder);

                string targetJarPath = Path.Combine(modsFolder, $"{modName}.jar");

                if (File.Exists(targetJarPath)) return;

                // Pass-through handler prevents local network handshakes from blocking mod binary acquisitions
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using var httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "TwistClient-Engine/1.0");

                byte[] fileBytes = await httpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(targetJarPath, fileBytes);

                System.Diagnostics.Debug.WriteLine($"[TwistEngine] Mod injected successfully: {modName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TwistEngine] Mod download failed: {ex.Message}");
                throw;
            }
        }
    }
}
