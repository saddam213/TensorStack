using CSnakesLite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Common;
using TensorStack.HuggingFace.Common;

namespace TensorStack.HuggingFace
{
    /// <summary>
    /// Install/Manage Python portable installation and virtual environment creation
    /// </summary>
    public class HuggingFaceBackend : Backend<PipelineProgress>, IVendorBackend
    {
        private ILogger _logger;
        private string _pythonPath;
        private string _pipelinePath;
        private PythonVersion _pythonVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="HuggingFaceBackend"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="baseDirectory">The base directory.</param>
        /// <param name="logger">The logger.</param>
        public HuggingFaceBackend(BackendConfig config, IProgress<PipelineProgress> progressCallback = null, ILogger logger = default)
            : base(config, progressCallback) => Initialize(logger);

        /// <summary>
        /// Initializes a new instance of the <see cref="HuggingFaceBackend"/> class.
        /// </summary>
        /// <param name="vendor">The vendor.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="logger">The logger.</param>
        public HuggingFaceBackend(VendorType vendor, IProgress<PipelineProgress> progressCallback = null, ILogger logger = default)
            : base(vendor, progressCallback) => Initialize(logger);

        /// <summary>
        /// Initializes the specified InstallManager.
        /// </summary>
        /// <param name="logger">The logger.</param>
        protected void Initialize(ILogger logger)
        {
            _logger = logger;
            _pythonVersion = GetPythonVresion(Config.Version);
            _pythonPath = Path.GetFullPath(Path.Join(Config.Directory, _pythonVersion.Folder));
            _pipelinePath = Path.GetFullPath(Path.Join(Config.Directory, "Pipelines"));
            CopyInternalPythonFiles();
        }

        /// <summary>
        /// Gets the backend.
        /// </summary>
        public override BackendType Type => BackendType.HuggingFace;

        /// <summary>
        /// Gets the directory.
        /// </summary>
        public override string GetDirectory()
        {
            return Path.Combine(_pipelinePath, Config.Name);
        }

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
        public override async Task<bool> DeleteAsync()
        {
            var path = GetDirectory();
            if (!Directory.Exists(path))
                return false;

            await Task.Run(() => Directory.Delete(path, true));
            return Exists();
        }


        /// <summary>
        /// Load an existing environment.
        /// </summary>
        /// <returns><c>true</c> if loaded, <c>false</c> otherwise.</returns>
        public override Task<bool> LoadAsync()
        {
            return LoadInternalAsync();
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
            LogProgress(new PipelineProgress { Key = "Initialize", Message = message });
        }


        /// <summary>
        /// Setup the Environment.
        /// </summary>
        /// <param name="status">The status.</param>
        private async Task<bool> SetupInternalAsync(BackendStatus status)
        {
            if (status == BackendStatus.Load)
                return Exists();

            var isRebuild = status == BackendStatus.Rebuild;
            var isReinstall = status == BackendStatus.Reinstall;
            await DownloadAsync(isReinstall);
            if (isReinstall || isRebuild)
                await DeleteAsync();

            return await CreateInternalAsync(status);
        }


        /// <summary>
        /// Creates an environment.
        /// </summary>
        private async Task<bool> CreateInternalAsync(BackendStatus status)
        {
            var requirementsFile = Path.Combine(_pipelinePath, "requirements.txt");
            try
            {
                LogProgress($"{status} Python Virtual Environment ({Config.Name})");
                await File.WriteAllLinesAsync(requirementsFile, Config.Requirements);
                PythonEnvironmentHelper.CreateEnvironment(Config.Name, _pythonPath, _pipelinePath, requirementsFile, _pythonVersion.Version, _logger);
                LogProgress($"Python Virtual Environment Configured.");
                return true;
            }
            finally
            {
                FileHelper.DeleteFile(requirementsFile);
            }
        }


        /// <summary>
        /// Load an existing Environment.
        /// </summary>
        /// <param name="progressCallback">The progress callback.</param>
        /// <returns>A Task&lt;IPythonEnvironment&gt; representing the asynchronous operation.</returns>
        /// <exception cref="System.Exception">Environment does not exist</exception>
        private async Task<bool> LoadInternalAsync()
        {
            if (!Exists())
                throw new Exception("Environment does not exist");

            return await Task.Run(() =>
            {
                LogProgress($"Loading Python Virtual Environment (.{Config.Name})");
                PythonEnvironmentHelper.CreateEnvironment(Config.Name, _pythonPath, _pipelinePath, _pythonVersion.Version, _logger);
                LogProgress($"Python Virtual Environment Loaded");
                return true;
            });
        }


        /// <summary>
        /// Downloads and installs Win-Python portable v3.12.10.
        /// </summary>
        /// <param name="reinstall">if set to <c>true</c> [reinstall].</param>
        private async Task DownloadAsync(bool reinstall)
        {
            var subfolder = $"{_pythonVersion.ZipFolder}/python";
            var exePath = Path.Combine(_pythonPath, "python.exe");
            var downloadPath = Path.Combine(_pythonPath, _pythonVersion.FileName);
            var pythonUrl = _pythonVersion.Link;
            if (reinstall)
            {
                LogProgress($"Reinstalling Python {_pythonVersion}...");
                if (File.Exists(downloadPath))
                    File.Delete(downloadPath);

                if (Directory.Exists(_pythonPath))
                    Directory.Delete(_pythonPath, true);

                LogProgress($"Python Uninstalled.");
            }

            // Create Python 
            Directory.CreateDirectory(_pythonPath);

            // Download Python
            if (!File.Exists(downloadPath))
            {
                LogProgress($"Download Python {_pythonVersion}...");
                using (var httpClient = new HttpClient())
                using (var response = await httpClient.GetAsync(pythonUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(stream);
                    }
                }
                LogProgress("Python Download Complete.");
            }

            // Extract ZIP file
            if (!File.Exists(exePath))
            {
                LogProgress($"Installing Python {_pythonVersion}...");
                CopyInternalPythonFiles();
                using (var archive = ZipFile.OpenRead(downloadPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.StartsWith(subfolder, StringComparison.OrdinalIgnoreCase))
                        {
                            var relativePath = entry.FullName.Replace('/', '\\').Substring(subfolder.Length);
                            if (string.IsNullOrWhiteSpace(relativePath))
                                continue;

                            var isDirectory = relativePath.EndsWith('\\');
                            var destinationPath = Path.Combine(_pythonPath, relativePath.TrimStart('\\').TrimEnd('\\'));
                            if (isDirectory)
                            {
                                Directory.CreateDirectory(destinationPath);
                                continue;
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            }
                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                }
                LogProgress($"Python Install Complete.");
            }
        }


        /// <summary>
        /// Copies the internal python files.
        /// </summary>
        private void CopyInternalPythonFiles()
        {
            ExtractFiles("Python", _pythonPath);
            ExtractFiles("Pipelines", _pipelinePath);
        }


        /// <summary>
        /// Gets the python vresion.
        /// </summary>
        /// <param name="versionId">The version identifier.</param>
        /// <exception cref="System.ArgumentException">Invalid PythonVersion '${versionId}', Expected: 3.12, 3.13 or 3.14</exception>
        private static PythonVersion GetPythonVresion(string versionId)
        {
            if (_supportedVersions.TryGetValue(versionId, out PythonVersion pythonVersion))
                return pythonVersion;

            throw new ArgumentException($"Invalid PythonVersion '${versionId}', Expected: 3.12, 3.13 or 3.14");
        }


        /// <summary>
        /// The supported Python versions
        /// </summary>
        private readonly static Dictionary<string, PythonVersion> _supportedVersions = new Dictionary<string, PythonVersion>
        {
            {"3.12", new PythonVersion("3.12.10", "https://github.com/winpython/winpython/releases/download/15.3.20250425final/Winpython64-3.12.10.0dot.zip", "Winpython64-3.12.10.0dot.zip", "WPy64-312100", "Python") },
            {"3.13", new PythonVersion("3.13.13", "https://github.com/winpython/winpython/releases/download/17.4.20260511final/WinPython64-3.13.13.0dot.zip", "WinPython64-3.13.13.0dot.zip", "WPy64-313130", "Python313") },
            {"3.14", new PythonVersion("3.14.5", "https://github.com/winpython/winpython/releases/download/17.4.20260511final/WinPython64-3.14.5.0dot.zip", "WinPython64-3.14.5.0dot.zip", "WPy64-31450", "Python314") }
        };


        /// <summary>
        /// Extracts the python pipeline files.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="destination">The destination.</param>
        private static void ExtractFiles(string resource, string destination)
        {
            var assembly = typeof(HuggingFaceBackend).Assembly;
            var assemblyName = assembly.GetName().Name;
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                var relativePath = default(string);
                var nativeNamespace = $"{assemblyName}.Native.{resource}.";
                if (resourceName.StartsWith(nativeNamespace, StringComparison.Ordinal))
                {
                    relativePath = resourceName[nativeNamespace.Length..].Replace('.', Path.DirectorySeparatorChar);
                }
                if (relativePath == null)
                    continue;

                var outputPath = Path.Combine(destination, SetExtension(relativePath));
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                using (var input = assembly.GetManifestResourceStream(resourceName))
                using (var output = File.Create(outputPath))
                {
                    input.CopyTo(output);
                }
            }

            static string SetExtension(string path)
            {
                var end = path.LastIndexOf('\\');
                var extension = path[end..].Trim('\\');
                return $"{path[..end]}.{extension}";
            }
        }


        /// <summary>
        /// The default HuggingFace python requirements
        /// </summary>
        public static readonly string[] DefaultRequirements =
        [
            "typing==3.7.4.3",
            "wheel==0.48.0",
            "transformers==5.19.0",
            "accelerate==1.15.0",
            "diffusers==0.41.0",
            "protobuf==7.36.2",
            "sentencepiece==0.2.2",
            "ftfy==6.3.1",
            "scipy==1.18.1",
            "peft==0.21.2",
            "torchsde==0.2.6",
            "gguf==0.19.0",
            "av==19.0.1",
            "soundfile==0.14.0",
            "optimum-quanto==0.2.7",
            "bitsandbytes==0.50.2"
        ];


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
                    Version = "3.12",
                    Name = "default-rocm",
                    Directory = "HuggingFace",
                    Variables = new Dictionary<string, string> {
                        {"MIOPEN_FIND_MODE", "2" },
                        {"HIP_VISIBLE_DEVICES", "0,1" },
                        {"TORCH_ROCM_AOTRITON_ENABLE_EXPERIMENTAL", "1" }
                    },
                    Requirements =
                    [
                        "https://repo.radeon.com/rocm/windows/rocm-rel-7.2.1/rocm-7.2.1.tar.gz",
                        "https://repo.radeon.com/rocm/windows/rocm-rel-7.2.1/rocm_sdk_core-7.2.1-py3-none-win_amd64.whl",
                        "https://repo.radeon.com/rocm/windows/rocm-rel-7.2.1/rocm_sdk_devel-7.2.1-py3-none-win_amd64.whl",
                        "https://repo.radeon.com/rocm/windows/rocm-rel-7.2.1/rocm_sdk_libraries_custom-7.2.1-py3-none-win_amd64.whl",
                        "https://repo.radeon.com/rocm/windows/rocm-rel-7.2.1/torch-2.9.1%2Brocm7.2.1-cp312-cp312-win_amd64.whl",
                        "https://repo.radeon.com/rocm/windows/rocm-rel-7.2.1/torchaudio-2.9.1%2Brocm7.2.1-cp312-cp312-win_amd64.whl",
                        "https://repo.radeon.com/rocm/windows/rocm-rel-7.2.1/torchvision-0.24.1%2Brocm7.2.1-cp312-cp312-win_amd64.whl",
                        ..DefaultRequirements,
                    ]
                },
                VendorType.Nvidia => new BackendConfig
                {
                    Version = "3.12",
                    Name = "default-cuda",
                    Directory = "HuggingFace",
                    Variables = new Dictionary<string, string> {
                        {"CUDA_VISIBLE_DEVICES", "0,1" },
                        {"DIFFUSERS_GGUF_CUDA_KERNELS", "true" }
                    },
                    Requirements =
                    [
                        "--extra-index-url https://download.pytorch.org/whl/cu130",
                        "torch==2.11.0+cu130",
                        "torchaudio==2.11.0+cu130",
                        "torchvision==0.26.0+cu130",
                        "torchao==0.18.0",
                        ..DefaultRequirements,
                    ]
                },
                _ => new BackendConfig
                {
                    Version = "3.12",
                    Name = "default-cpu",
                    Directory = "HuggingFace",
                    Requirements = [
                        "torch==2.9.1",
                        "torchvaudio==2.9.1",
                        "torchvision==0.24.1",
                        "torchao==0.18.0",
                        ..DefaultRequirements,
                    ]
                },
            };
        }

        private record PythonVersion(string Version, string Link, string FileName, string ZipFolder, string Folder);
    }
}
