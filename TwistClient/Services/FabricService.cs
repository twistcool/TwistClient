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

            return await installer.Install(
                minecraftVersion,
                _minecraftService.MinecraftPath
            );
        }

        public string GetFabricVersion(string minecraftVersion)
        {
            if (string.IsNullOrWhiteSpace(minecraftVersion) || minecraftVersion.Contains("1.8") || minecraftVersion.Contains("1.12"))
            {
                return minecraftVersion;
            }

            return $"fabric-loader-{minecraftVersion}";
        }
    }
}
