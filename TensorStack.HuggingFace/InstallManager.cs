using CSnakes.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Common;
using TensorStack.Common.Pipeline;
using TensorStack.HuggingFace.Common;

namespace TensorStack.HuggingFace
{
    /// <summary>
    /// PythonManager - Manage Python portable installation and virtual environment creation
    /// </summary>
    public class InstallManager : EnvironmentInstaller
    {
        private readonly ILogger _logger;
        private readonly string _pythonPath;
        private readonly string _pipelinePath;
        private readonly PythonVersion _pythonVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallManager"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="baseDirectory">The base directory.</param>
        /// <param name="logger">The logger.</param>
        public InstallManager(EnvironmentConfig config, IProgress<RunProgress> progressCallback = null, ILogger logger = default)
            : base(config, progressCallback)
        {
            _logger = logger;
            _pythonVersion = GetPythonVresion(config.Version);
            _pythonPath = Path.GetFullPath(Path.Join(Config.Directory, _pythonVersion.Folder));
            _pipelinePath = Path.GetFullPath(Path.Join(Config.Directory, "Pipelines"));
            CopyInternalPythonFiles();
            CopyInternalPipelineFiles();
        }


        /// <summary>
        /// Checks if a environment exists
        /// </summary>
        /// <returns><c>true</c> if exists, <c>false</c> otherwise.</returns>
        public override bool Exists()
        {
            var path = Path.Combine(_pipelinePath, $".{Config.Name}");
            return Directory.Exists(path);
        }


        /// <summary>
        /// Delete an environment
        /// </summary>
        /// <returns><c>true</c> if deleted, <c>false</c> otherwise.</returns>
        public override async Task<bool> DeleteAsync()
        {
            var path = Path.Combine(_pipelinePath, $".{Config.Name}");
            if (!Directory.Exists(path))
                return false;

            await Task.Run(() => Directory.Delete(path, true));
            return Exists();
        }


        /// <summary>
        /// Load an existing environment.
        /// </summary>
        /// <returns><c>true</c> if loaded, <c>false</c> otherwise.</returns>
        public override async Task<bool> LoadAsync()
        {
            return await LoadInternalAsync();
        }


        /// <summary>
        /// Creates the Environment.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns><c>true</c> if created/updated, <c>false</c> otherwise.</returns>
        public override async Task<bool> CreateAsync(EnvironmentStatus status)
        {
            return await Task.Run(async () =>
            {
                var isRebuild = status == EnvironmentStatus.Rebuild;
                var isReinstall = status == EnvironmentStatus.Reinstall;
                await DownloadAsync(isReinstall);
                if (isReinstall || isRebuild)
                    await DeleteAsync();

                return await CreateInternalAsync(status);
            });
        }


        /// <summary>
        /// Creates an environment.
        /// </summary>
        private async Task<bool> CreateInternalAsync(EnvironmentStatus status, IProgress<PipelineProgress> progressCallback = null)
        {
            var requirementsFile = Path.Combine(_pipelinePath, "requirements.txt");
            try
            {
                LogProgress($"{status} Python Virtual Environment (.{Config.Name})");
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
        private async Task<bool> LoadInternalAsync(IProgress<PipelineProgress> progressCallback = null)
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
        private async Task DownloadAsync(bool reinstall, IProgress<PipelineProgress> progressCallback = null)
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
        }


        /// <summary>
        /// Copies the internal pipeline files.
        /// </summary>
        private void CopyInternalPipelineFiles()
        {
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
            var assembly = typeof(InstallManager).Assembly;
            var assemblyName = assembly.GetName().Name;
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                var relativePath = default(string);
                var pythonRuntimeNamespace = $"{assemblyName}.PythonRuntime.{resource}.";
                if (resourceName.StartsWith(pythonRuntimeNamespace, StringComparison.Ordinal))
                {
                    relativePath = resourceName[pythonRuntimeNamespace.Length..].Replace('.', Path.DirectorySeparatorChar);
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
    }

    public record PythonVersion(string Version, string Link, string FileName, string ZipFolder, string Folder);
}
