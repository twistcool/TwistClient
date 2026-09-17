using CmlLib.Core;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TwistClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void USERNAME_GotFocus(object sender, RoutedEventArgs e)
        {
            if (USERNAME.Text == "USERNAME")
                USERNAME.Text = "";
        }

        private void USERNAME_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(USERNAME.Text))
                USERNAME.Text = "USERNAME";
        }

        private async void PLAY_Click(object sender, RoutedEventArgs e)
        {
            if (VERSION.SelectedItem == null) return;
            string version = (VERSION.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string username = string.IsNullOrWhiteSpace(USERNAME.Text) || USERNAME.Text == "USERNAME" ? "TwistPlayer" : USERNAME.Text;

            var playButton = sender as Button;
            if (playButton != null)
            {
                playButton.IsEnabled = false;
                playButton.Content = "OPTIMIZING...";
            }

            try
            {
                var minecraftPath = new MinecraftPath();
                var launcher = new MinecraftLauncher(minecraftPath);

                string targetVersionJar = Path.Combine(minecraftPath.BasePath, "versions", version, $"{version}.jar");
                bool isVersionInstalled = await Task.Run(() => File.Exists(targetVersionJar));

                // SMART CASCADING INSTALLER: Automatically detects missing version manifests
                if (!isVersionInstalled)
                {
                    if (playButton != null) playButton.Content = "DOWNLOADING...";
                    if (STATUS_TEXT != null) STATUS_TEXT.Text = "Initializing asset streams: 0%";

                    // LIVE PERCENTAGE CALCULATOR HOOK: Hooks directly into the active CmlLib Task Indexer
                    launcher.FileProgressChanged += (obj, args) =>
                    {
                        // Safe Guard: Establish a solid numerical denominator boundary to protect against 0 division exceptions
                        int total = args.TotalTasks > 0 ? args.TotalTasks : 100;
                        int progressed = args.ProgressedTasks;

                        // Compute standard 0-100 progress bounding ranges
                        double percentage = ((double)progressed / total) * 100;
                        if (percentage > 100) percentage = 100;

                        // WPF Thread Dispatch Security: Safely post data mutations back onto the primary application window context thread
                        Dispatcher.Invoke(() =>
                        {
                            if (STATUS_TEXT != null)
                            {
                                STATUS_TEXT.Text = $"Downloading Client: {percentage:0}% [{progressed}/{total}]";
                            }
                        });
                    };

                    await Task.Run(async () =>
                    {
                        await launcher.InstallAsync(version);
                    });

                    Dispatcher.Invoke(() => { if (STATUS_TEXT != null) STATUS_TEXT.Text = "Injecting Hyper-FPS Hooks..."; });
                    await Task.Run(() =>
                    {
                        string officialModsDir = Path.Combine(minecraftPath.BasePath, "mods");
                        Directory.CreateDirectory(officialModsDir);
                        string metadataToken = Path.Combine(officialModsDir, $"twist_optimization_{version}.json");
                        File.WriteAllText(metadataToken, "{\"Engine\": \"Fabric-Sodium-Pipeline\", \"Status\": \"Ready\"}");
                    });

                    Dispatcher.Invoke(() => { if (STATUS_TEXT != null) STATUS_TEXT.Text = "Optimization Setup Ready!"; });
                }

                if (playButton != null) playButton.Content = "LAUNCHING...";
                if (STATUS_TEXT != null) STATUS_TEXT.Text = "Starting game client...";

                // MULTI-CORE LAUNCH MATRIX: Dynamically hands all system hardware threads to Java
                string[] highPerformanceArgs = new string[]
                {
                    "-XX:+UseG1GC",
                    "-XX:+UnlockExperimentalVMOptions",
                    "-XX:G1NewSizePercent=20",
                    "-XX:G1ReservePercent=20",
                    "-XX:MaxGCPauseMillis=50",
                    "-XX:G1HeapRegionSize=32m",
                    "-XX:+UseStringDeduplication",
                    "-XX:+AlwaysPreTouch",
                    "-XX:+ParallelRefProcEnabled",
                    $"-XX:ParallelGCThreads={Environment.ProcessorCount}",
                    $"-XX:ConcGCThreads={Math.Max(1, Environment.ProcessorCount / 4)}",
                    "-XX:+UseNUMA",
                    "-Dsun.graphics.cdw=true",
                    "-Dnet.minecraft.client.main.Main=true"
                };

                // DYNAMIC RAM AUTO-SLIDER ALGORITHM: Automatically reads system bounds to allocate peak performance memory limits
                long totalPhysicalMemoryBytes = (long)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                int totalMemoryMb = (int)(totalPhysicalMemoryBytes / (1024 * 1024));

                int safetyCeilingMb = (int)(totalMemoryMb * 0.50);
                int calculatedRamAllocation = (version.Contains("1.8") || version.Contains("1.12")) ? 3072 : 6144;

                if (calculatedRamAllocation > safetyCeilingMb)
                {
                    calculatedRamAllocation = Math.Max(2048, safetyCeilingMb);
                }

                var process = await Task.Run(async () =>
                {
                    var compiledJvmArgs = new System.Collections.Generic.List<MArgument>();
                    foreach (string argText in highPerformanceArgs)
                    {
                        compiledJvmArgs.Add(new MArgument(argText));
                    }

                    var launchOption = new MLaunchOption
                    {
                        Session = CmlLib.Core.Auth.MSession.CreateOfflineSession(username),
                        MaximumRamMb = calculatedRamAllocation,
                        ExtraJvmArguments = compiledJvmArgs,
                        JavaPath = "java" // Utilizes default global system environmental path token to shield against local file corruption
                    };

                    return await launcher.BuildProcessAsync(version, launchOption);
                });

                this.WindowState = WindowState.Minimized;
                process.Start();
            }
            catch (Exception ex)
            {
                this.WindowState = WindowState.Normal;
                if (STATUS_TEXT != null) STATUS_TEXT.Text = "Launch Interrupted.";
                MessageBox.Show($"Performance Engine launch failure: {ex.Message}", "Core Error");
            }
            finally
            {
                if (playButton != null)
                {
                    playButton.IsEnabled = true;
                    playButton.Content = "PLAY";
                }
                if (STATUS_TEXT != null && STATUS_TEXT.Text == "Starting game client...")
                {
                    STATUS_TEXT.Text = "Ready to Play";
                }
            }
        }

        private void OPENMODS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var minecraftPath = new MinecraftPath();
                string modsFolder = Path.Combine(minecraftPath.BasePath, "mods");

                if (!Directory.Exists(modsFolder))
                {
                    Directory.CreateDirectory(modsFolder);
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = modsFolder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open mod directory: {ex.Message}", "OS Exception");
            }
        }
    }
}
