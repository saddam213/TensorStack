using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using TensorStack.AudioCpp.Common;
using TensorStack.AudioCpp.Native;
using TensorStack.Common;
using TensorStack.Common.Common;

namespace TensorStack.AudioCpp
{
    /// <summary>
    /// Install/Manage audio.cpp environment
    /// </summary>
    public class AudioCppBackend : Backend<PipelineProgress>, IVendorBackend
    {
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioCppBackend"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="logger">The logger.</param>
        public AudioCppBackend(BackendConfig config, IProgress<PipelineProgress> progressCallback = null, ILogger logger = default)
            : base(config, progressCallback) => Initialize(logger);

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioCppBackend"/> class.
        /// </summary>
        /// <param name="vendor">The vendor.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="logger">The logger.</param>
        public AudioCppBackend(VendorType vendor, IProgress<PipelineProgress> progressCallback = null, ILogger logger = default)
            : base(vendor, progressCallback) => Initialize(logger);

        /// <summary>
        /// Initializes the specified InstallManager.
        /// </summary>
        /// <param name="logger">The logger.</param>
        protected void Initialize(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the backend.
        /// </summary>
        public override BackendType Type => BackendType.AudioCpp;


        /// <summary>
        /// Checks if a environment exists
        /// </summary>
        /// <returns><c>true</c> if exists, <c>false</c> otherwise.</returns>
        public override bool Exists()
        {
            return Directory.Exists(GetDirectory());
        }


        /// <summary>
        /// Delete an environment
        /// </summary>
        /// <returns><c>true</c> if deleted, <c>false</c> otherwise.</returns>
        public override Task<bool> DeleteAsync()
        {
            if (!Exists())
                return Task.FromResult(false);

            FileHelper.DeleteDirectory(GetDirectory());
            return Task.FromResult(true);
        }


        /// <summary>
        /// Load an existing environment.
        /// </summary>
        /// <returns><c>true</c> if loaded, <c>false</c> otherwise.</returns>
        public override Task<bool> LoadAsync()
        {
            try
            {
                return Task.FromResult(NativeApi.LoadNativeLibrary(out _, GetDirectory()));
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }


        /// <summary>
        /// Setup the Environment.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns><c>true</c> if created/updated, <c>false</c> otherwise.</returns>
        public override Task<bool> SetupAsync(BackendStatus status)
        {
            return Task.Run(() => SetupInternalAsync(status));
        }


        /// <summary>
        /// Gets the default vendor configuration.
        /// </summary>
        /// <param name="vendor">The vendor.</param>
        /// <returns>EnvironmentConfig.</returns>
        protected override BackendConfig GetVendorConfig(VendorType vendor)
        {
            return GetConfig(vendor);
        }


        /// <summary>
        /// Logs the progress message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void LogProgress(string message)
        {
            _logger?.LogInformation(message);
            LogProgress(new PipelineProgress { Key = "Initialize", Message = message });
        }


        /// <summary>
        /// Setup the Environment.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns>A Task&lt;System.Boolean&gt; representing the asynchronous operation.</returns>
        private async Task<bool> SetupInternalAsync(BackendStatus status)
        {
            if (status == BackendStatus.Load)
                return Exists();

            var directory = GetDirectory();
            var tempDirectory = Path.Combine(directory, "temp");
            try
            {
                if (status == BackendStatus.Update || status == BackendStatus.Rebuild || status == BackendStatus.Reinstall)
                {
                    LogProgress("Uninstall Environment...");
                    FileHelper.DeleteDirectory(directory);
                    LogProgress("Environment Uninstalled.");
                }

                if (Exists())
                {
                    LogProgress("Environment Already Installed.");
                    return true;
                }

                Directory.CreateDirectory(tempDirectory);

                // Download audio.cpp Requirements
                await DownloadRequirementsAsync(Config.Requirements, tempDirectory);

                // Install audio.cpp Requirements
                await InstallRequirementsAsync(Config.Requirements, directory, tempDirectory);

                // Verify audio.cpp Install
                if (!Exists())
                {
                    LogProgress("Environment Install Failed.");
                    FileHelper.DeleteDirectory(directory);
                    return false;
                }

                LogProgress("Environment Install Complete.");
                return true;
            }
            finally
            {
                FileHelper.DeleteDirectory(tempDirectory);
            }
        }


        /// <summary>
        /// Download environment requirements
        /// </summary>
        /// <param name="requirements">The requirements.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task DownloadRequirementsAsync(string[] requirements, string workingDirectory)
        {
            LogProgress("Download Environment...");
            using (var httpClient = new HttpClient())
            {
                foreach (var requirement in requirements)
                {
                    var name = Path.GetFileName(requirement);
                    var destination = Path.Combine(workingDirectory, name);
                    LogProgress($"Downloading {name}...");
                    await DownloadFileAsync(httpClient, requirement, destination);
                }
            }
            LogProgress("Download Environment Complete.");
        }


        /// <summary>
        /// Install environment requirements
        /// </summary>
        /// <param name="requirements">The requirements.</param>
        /// <param name="environmentDirectory">The environment directory.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private async Task InstallRequirementsAsync(string[] requirements, string environmentDirectory, string workingDirectory)
        {
            LogProgress("Installing Environment...");
            Directory.CreateDirectory(environmentDirectory);
            foreach (var requirement in requirements)
            {
                var filename = Path.GetFileName(requirement);
                var requirementFile = Path.Combine(workingDirectory, filename);
                if (!File.Exists(requirementFile))
                    continue;

                LogProgress($"Unpacking {filename}...");
                if (Path.GetExtension(requirementFile).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    await ExtractZipAsync(requirementFile, environmentDirectory);
                    FileHelper.DeleteFile(requirementFile);
                }
                else
                {
                    File.Move(requirementFile, Path.Combine(environmentDirectory, filename), true);
                }
            }
        }


        /// <summary>
        /// Extracts the zip contents.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="destinationDirectory">The destination directory.</param>
        private static async Task ExtractZipAsync(string sourceFile, string destinationDirectory)
        {
            var rootDirectory = Path.GetFullPath(destinationDirectory) + Path.DirectorySeparatorChar;
            using (var archive = ZipFile.OpenRead(sourceFile))
            {
                foreach (var entry in archive.Entries)
                {
                    var relativePath = entry.FullName.Replace('/', '\\').TrimStart('\\');
                    if (string.IsNullOrWhiteSpace(relativePath))
                        continue;

                    var destination = Path.GetFullPath(Path.Combine(rootDirectory, Path.GetFileName(relativePath))); // Note: Flattened file tree
                    if (!destination.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase))
                        throw new IOException($"Archive entry '{entry.FullName}' is outside the environment directory.");

                    if (relativePath.EndsWith('\\'))
                    {
                        Directory.CreateDirectory(destination);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    await entry.ExtractToFileAsync(destination, overwrite: true);
                }
            }
        }


        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="sourceUrl">The source URL.</param>
        /// <param name="destinationFile">The destination file.</param>
        private static async Task DownloadFileAsync(HttpClient httpClient, string sourceUrl, string destinationFile)
        {
            const int bufferSize = 1024 * 1024;
            using (var response = await httpClient.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using (var inputStream = await response.Content.ReadAsStreamAsync())
                await using (var outputStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: bufferSize, useAsync: true))
                {
                    await inputStream.CopyToAsync(outputStream, bufferSize);
                }
            }
        }


        /// <summary>
        /// Gets the Vendor default configuration.
        /// </summary>
        /// <param name="vendor">The vendor.</param>
        public static BackendConfig GetConfig(VendorType vendor)
        {
            return vendor switch
            {
                VendorType.Nvidia => new BackendConfig
                {
                    Name = "cuda",
                    Version = "95",
                    Directory = "AudioCpp",
                    Requirements =
                    [
                        "https://github.com/0xShug0/audio.cpp/releases/download/v0.9.5/audio-v0.9.5-lib-windows-x64-cuda12.4.zip",
                        "https://github.com/0xShug0/audio.cpp/releases/download/v0.9.5/audio-v0.9.5-cudart-windows-x64-cuda12.4.zip"
                    ]
                },
                _ => new BackendConfig
                {
                    Name = "vulkan",
                    Version = "95",
                    Directory = "AudioCpp",
                    Requirements =
                    [
                        "https://github.com/0xShug0/audio.cpp/releases/download/v0.9.5/audio-v0.9.5-lib-windows-x64-vulkan.zip"
                    ]
                },
            };
        }
    }
}
