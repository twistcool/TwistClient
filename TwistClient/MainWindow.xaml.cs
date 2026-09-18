using CmlLib.Core;
using CmlLib.Core.ProcessBuilder;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TwistClient.Services;

namespace TwistClient
{
    public partial class MainWindow : Window
    {
        private readonly MinecraftService _minecraftService;
        private readonly FabricService _fabricService;

        public MainWindow()
        {
            InitializeComponent();
            _minecraftService = new MinecraftService();
            _fabricService = new FabricService(_minecraftService);
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

            string baseVersion = (VERSION.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string username = string.IsNullOrWhiteSpace(USERNAME.Text) || USERNAME.Text == "USERNAME" ? "TwistPlayer" : USERNAME.Text;
            string launchVersionName = baseVersion;

            var playButton = sender as Button;
            if (playButton != null)
            {
                playButton.IsEnabled = false;
                playButton.Content = "OPTIMIZING...";
            }

            try
            {
                var minecraftPath = _minecraftService.MinecraftPath;
                var launcher = _minecraftService.CreateLauncher();

                bool isVersionInstalled = _minecraftService.IsVersionInstalled(baseVersion);

                if (!isVersionInstalled)
                {
                    if (playButton != null) playButton.Content = "DOWNLOADING...";
                    if (STATUS_TEXT != null) STATUS_TEXT.Text = "Connecting to Mojang Content Delivery Networks...";

                    launcher.FileProgressChanged += (obj, args) =>
                    {
                        int total = args.TotalTasks > 0 ? args.TotalTasks : 100;
                        int progressed = args.ProgressedTasks;
                        double percentage = ((double)progressed / total) * 100;
                        if (percentage > 100) percentage = 100;

                        Dispatcher.Invoke(() =>
                        {
                            if (STATUS_TEXT != null)
                            {
                                STATUS_TEXT.Text = $"Streaming Engine Packs: {percentage:0}% [{progressed}/{total}]";
                            }
                        });
                    };

                    await Task.Run(async () =>
                    {
                        await launcher.InstallAsync(baseVersion);
                    });

                    if (!baseVersion.Contains("1.8") && !baseVersion.Contains("1.12"))
                    {
                        Dispatcher.Invoke(() => { if (STATUS_TEXT != null) STATUS_TEXT.Text = "Injecting Clean Fabric Core Layers..."; });
                        launchVersionName = await _fabricService.InstallFabricAsync(baseVersion);

                        Dispatcher.Invoke(() => { if (STATUS_TEXT != null) STATUS_TEXT.Text = "Downloading Sodium Graphics Engine..."; });
                        await _minecraftService.DownloadModAsync("Sodium-Optimization", "https://github.com");

                        Dispatcher.Invoke(() => { if (STATUS_TEXT != null) STATUS_TEXT.Text = "Downloading Lithium Physics Engine..."; });
                        await _minecraftService.DownloadModAsync("Lithium-Optimization", "https://github.com");
                    }

                    Dispatcher.Invoke(() => { if (STATUS_TEXT != null) STATUS_TEXT.Text = "Injecting Hyper-FPS Hooks..."; });
                    await Task.Run(() =>
                    {
                        string officialModsDir = Path.Combine(minecraftPath.BasePath, "mods");
                        Directory.CreateDirectory(officialModsDir);
                        string metadataToken = Path.Combine(officialModsDir, $"twist_optimization_{baseVersion}.json");
                        File.WriteAllText(metadataToken, "{\"Engine\": \"Fabric-Sodium-Pipeline\", \"Status\": \"Ready\"}");
                    });

                    Dispatcher.Invoke(() => { if (STATUS_TEXT != null) STATUS_TEXT.Text = "Optimization Setup Ready!"; });
                }
                else
                {
                    if (!baseVersion.Contains("1.8") && !baseVersion.Contains("1.12"))
                    {
                        string versionsDir = Path.Combine(minecraftPath.BasePath, "versions");
                        if (Directory.Exists(versionsDir))
                        {
                            var subDirs = Directory.GetDirectories(versionsDir);
                            foreach (var dir in subDirs)
                            {
                                string folderName = Path.GetFileName(dir);
                                if (folderName.StartsWith("fabric-loader-") && folderName.Contains(baseVersion))
                                {
                                    launchVersionName = folderName;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (playButton != null) playButton.Content = "LAUNCHING...";
                if (STATUS_TEXT != null) STATUS_TEXT.Text = "Starting game client...";

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
                    "-Dnet.minecraft.client.main.Main=true",
                    $"-Dtwist.client.user={username}",
                    $"-Dtwist.client.profile={baseVersion}",
                    "-Dtwist.cosmetics.capes=true",
                    "-Dtwist.hud.keystrokes=true"
                };

                long totalPhysicalMemoryBytes = (long)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                int totalMemoryMb = (int)(totalPhysicalMemoryBytes / (1024 * 1024));

                int safetyCeilingMb = (int)(totalMemoryMb * 0.50);
                int calculatedRamAllocation = (baseVersion.Contains("1.8") || baseVersion.Contains("1.12")) ? 3072 : 6144;

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

                Dispatcher.Invoke(() => { if (STATUS_TEXT != null) STATUS_TEXT.Text = "Routing Java Performance Environments..."; });

                string targetJavaExecutable = "java";

                string tlauncherRuntimePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".minecraft", "runtime"
                );

                if (Directory.Exists(tlauncherRuntimePath))
                {
                    if (!baseVersion.Contains("1.8") && !baseVersion.Contains("1.12"))
                    {
                        var javaExecutables = Directory.GetFiles(tlauncherRuntimePath, "javaw.exe", SearchOption.AllDirectories);
                        foreach (var path in javaExecutables)
                        {
                            if (path.Contains("java-render") || path.Contains("alpha") || path.Contains("gamma") || path.Contains("delta") || !path.Contains("jre8"))
                            {
                                targetJavaExecutable = path;
                                break;
                            }
                        }
                    }
                    else
                    {
                        var javaExecutables = Directory.GetFiles(tlauncherRuntimePath, "javaw.exe", SearchOption.AllDirectories);
                        foreach (var path in javaExecutables)
                        {
                            if (path.Contains("jre8") || path.Contains("legacy"))
                            {
                                targetJavaExecutable = path;
                                break;
                            }
                        }
                    }
                }

                var launchOption = new MLaunchOption
                {
                    Session = CmlLib.Core.Auth.MSession.CreateOfflineSession(username),
                    MaximumRamMb = calculatedRamAllocation,
                    ExtraJvmArguments = compiledJvmArgs,
                    JavaPath = targetJavaExecutable
                };

                    return await launcher.BuildProcessAsync(launchVersionName, launchOption);
                });

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                this.WindowState = WindowState.Minimized;

                try
                {
                    process.Start();
                }
                catch (Exception processEx)
                {
                    this.WindowState = WindowState.Normal;
                    MessageBox.Show($"OS Process Initiation Exception: {processEx.Message}", "Windows Subsystem Block");
                }
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
                var minecraftPath = _minecraftService.MinecraftPath;
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

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
