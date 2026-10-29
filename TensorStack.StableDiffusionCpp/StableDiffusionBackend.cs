using Microsoft.Extensions.Logging;
using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Common;
using TensorStack.StableDiffusionCpp.Common;
using TensorStack.StableDiffusionCpp.Native;

namespace TensorStack.StableDiffusionCpp
{
    /// <summary>
    /// Install/Manage StableDiffusion.cpp environment 
    /// </summary>
    public class StableDiffusionBackend : Backend<PipelineProgress>, IVendorBackend
    {
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionBackend"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="logger">The logger.</param>
        public StableDiffusionBackend(BackendConfig config, IProgress<PipelineProgress> progressCallback = null, ILogger logger = default)
            : base(config, progressCallback) => Initialize(logger);

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionBackend"/> class.
        /// </summary>
        /// <param name="vendor">The vendor.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="logger">The logger.</param>
        public StableDiffusionBackend(VendorType vendor, IProgress<PipelineProgress> progressCallback = null, ILogger logger = default)
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
        public override BackendType Type => BackendType.StableDiffusionCpp;


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
                var applicationPath = Path.Combine(directory, $"{NativeApi.LibraryName}.dll");
                if (status == BackendStatus.Update || status == BackendStatus.Rebuild || status == BackendStatus.Reinstall)
                {
                    LogProgress("Uninstall Environment...");
                    FileHelper.DeleteDirectory(directory);
                    LogProgress("Environment Uninstalled.");
                }

                if (File.Exists(applicationPath))
                {
                    LogProgress("Environment Already Installed.");
                    return true;
                }

                Directory.CreateDirectory(tempDirectory);

                // Download StableDiffusion.cpp Requirements
                await DownloadRequirementsAsync(Config.Requirements, tempDirectory);

                // Install StableDiffusion.cpp Requirements
                await InstallRequirementsAsync(Config.Requirements, directory, tempDirectory);

                // Verify StableDiffusion.cpp Install
                if (!File.Exists(applicationPath))
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
        /// <param name="progressCallback">The progress callback.</param>
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
        /// <param name="progressCallback">The progress callback.</param>
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
                await UnpackRequirementAsync(requirementFile, environmentDirectory, workingDirectory);
            }
        }


        /// <summary>
        /// Unpacks the requirement.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="environmentDirectory">The environment directory.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private static async Task UnpackRequirementAsync(string sourceFile, string environmentDirectory, string workingDirectory)
        {
            var extension = Path.GetExtension(sourceFile);
            if (extension.Equals(".zip"))
            {
                await ExtractZipAsync(sourceFile, environmentDirectory);
                FileHelper.DeleteFile(sourceFile);
            }
            else if (extension.Equals(".gz"))
            {
                await ExtractTarGzAsync(sourceFile, environmentDirectory, workingDirectory);
                FileHelper.DeleteFile(sourceFile);
            }
            else
            {
                File.Move(sourceFile, Path.Combine(environmentDirectory, Path.GetFileName(sourceFile)));
            }
        }


        /// <summary>
        /// Extracts the zip contents.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="environmentDirectory">The environment directory.</param>
        private static async Task ExtractZipAsync(string sourceFile, string environmentDirectory)
        {
            using (var archive = ZipFile.OpenRead(sourceFile))
            {
                foreach (var entry in archive.Entries)
                {
                    var relativePath = entry.FullName.Replace('/', '\\');
                    if (string.IsNullOrWhiteSpace(relativePath))
                        continue;

                    var filename = relativePath.TrimStart('\\').TrimEnd('\\');
                    var destination = Path.Combine(environmentDirectory, filename);
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
        /// Extract tar gz contents.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="environmentDirectory">The environment directory.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private static async Task ExtractTarGzAsync(string sourceFile, string environmentDirectory, string workingDirectory)
        {
            using (var file = File.OpenRead(sourceFile))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            {
                if (IsBinFolderOnly(sourceFile))
                {
                    var tempDirectory = CreateTempDirectory(workingDirectory);
                    try
                    {
                        await TarFile.ExtractToDirectoryAsync(gzip, tempDirectory, overwriteFiles: true);
                        var binDirectory = FileHelper.FindDirectory(tempDirectory, "bin");
                        if (binDirectory?.Exists == true)
                        {
                            MoveDirectoryContents(binDirectory, environmentDirectory);
                        }
                    }
                    finally
                    {
                        FileHelper.DeleteDirectory(tempDirectory);
                    }
                }
                else
                {
                    await TarFile.ExtractToDirectoryAsync(gzip, environmentDirectory, overwriteFiles: true);
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
        /// Moves the directory contents.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        private static void MoveDirectoryContents(DirectoryInfo source, string destination)
        {
            foreach (var file in source.EnumerateFiles())
            {
                file.MoveTo(Path.Combine(destination, file.Name), true);
            }

            foreach (var directory in source.EnumerateDirectories())
            {
                directory.MoveTo(Path.Combine(destination, directory.Name));
            }
        }


        /// <summary>
        /// Creates the temporary directory.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <returns>System.String.</returns>
        private static string CreateTempDirectory(string directory)
        {
            var tempDirectory = new DirectoryInfo(Path.Combine(directory, Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));
            tempDirectory.Create();
            return tempDirectory.FullName;
        }


        /// <summary>
        /// Determines whether to extract just the bin folder from the archive
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <returns><c>true</c> if only extract bin contents; otherwise, <c>false</c>.</returns>
        private static bool IsBinFolderOnly(string sourceFile)
        {
            return sourceFile.Contains("therock-dist-windows");
        }


        /// <summary>
        /// Gets the Vendor default configuration.
        /// </summary>
        /// <param name="vendor">The vendor.</param>
        public static BackendConfig GetConfig(VendorType vendor)
        {
            return vendor switch
            {
                VendorType.AMD => new BackendConfig
                {
                    Name = "rocm",
                    Version = "1024",
                    Directory = "StableDiffusionCpp",
                    Requirements =
                    [
                        "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-1024-77b72a2/sd-master-77b72a2-bin-win-rocm-10.1.0-x64.zip",
                        "https://stable.repo.amd.com/rocm/core/tarball/therock-dist-windows-multiarch-10.1.0.tar.gz"
                    ]
                },
                VendorType.Nvidia => new BackendConfig
                {
                    Name = "cuda",
                    Version = "1024",
                    Directory = "StableDiffusionCpp",
                    Requirements =
                    [
                        "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-1024-77b72a2/cudart-sd-bin-win-cu12-x64.zip",
                        "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-1024-77b72a2/sd-master-77b72a2-bin-win-cuda12-x64.zip"
                    ]
                },
                _ => new BackendConfig
                {
                    Name = "vulkan",
                    Version = "1024",
                    Directory = "StableDiffusionCpp",
                    Requirements =
                    [
                        "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-1024-77b72a2/sd-master-77b72a2-bin-win-vulkan-x64.zip"
                    ]
                },
            };
        }
    }
}
