using CmlLib.Core;
using CmlLib.Core.ModLoaders.FabricMC;
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

            // FIXED FOR V4: Reverted back to the official CmlLib method signature '.Install'
            return await installer.Install(
                minecraftVersion,
                _minecraftService.MinecraftPath
            );
        }

        // Dynamically maps out the official Fabric runtime profile identity names
        public string GetFabricVersion(string minecraftVersion)
        {
            if (string.IsNullOrWhiteSpace(minecraftVersion) || minecraftVersion.Contains("1.8") || minecraftVersion.Contains("1.12"))
            {
                return minecraftVersion; // Legacy builds run straight on optimized native OptiFine modules
            }

            // Modern formats match standard layout directory string definitions: fabric-loader-{loader}-{game}
            return $"fabric-loader-{minecraftVersion}";
        }
    }
}
