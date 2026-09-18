using CmlLib.Core;
using CmlLib.Core.ModLoaders.FabricMC;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace TwistClient.Services
{
    public class FabricService
    {
        private readonly MinecraftService _minecraftService;

        public FabricService(MinecraftService minecraftService)
        {
            _minecraftService = minecraftService;
        }

        public async Task<string> InstallFabricAsync(string minecraftVersion)
        {
            using var httpClient = new HttpClient();
            var installer = new FabricInstaller(httpClient);

            // 🚀 FORCE ISOLATION MATRIX: Download a fresh profile completely separate from TLauncher
            await installer.Install(
                minecraftVersion,
                _minecraftService.MinecraftPath
            );

            // Look inside the versions folder and find what folder CmlLib just built
            string versionsDir = Path.Combine(_minecraftService.MinecraftPath.BasePath, "versions");
            if (Directory.Exists(versionsDir))
            {
                var subDirs = Directory.GetDirectories(versionsDir);
                foreach (var dir in subDirs)
                {
                    string folderName = Path.GetFileName(dir);
                    // Match the freshly generated official fabric installer naming structure
                    if (folderName.StartsWith("fabric-loader-") && folderName.Contains(minecraftVersion))
                    {
                        return folderName;
                    }
                }
            }

            return $"fabric-loader-{minecraftVersion}";
        }

        public string GetFabricVersion(string minecraftVersion)
        {
            return minecraftVersion;
        }
    }
}
