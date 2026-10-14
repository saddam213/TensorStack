using System;
using System.Threading.Tasks;
using TensorStack.Common.Pipeline;

namespace TensorStack.Common
{
    public abstract class EnvironmentInstaller
    {
        private readonly EnvironmentConfig _config;
        private readonly IProgress<RunProgress> _progressCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentInstaller"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="progressCallback">The progress callback.</param>
        protected EnvironmentInstaller(EnvironmentConfig config, IProgress<RunProgress> progressCallback = null)
        {
            _config = config;
            _progressCallback = progressCallback;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public EnvironmentConfig Config => _config;

        /// <summary>
        /// Checks if a environment exists
        /// </summary>
        /// <returns><c>true</c> if exists, <c>false</c> otherwise.</returns>
        public abstract bool Exists();

        /// <summary>
        /// Delete an environment
        /// </summary>
        /// <returns><c>true</c> if deleted, <c>false</c> otherwise.</returns>
        public abstract Task<bool> DeleteAsync();

        /// <summary>
        /// Load an existing environment.
        /// </summary>
        /// <returns><c>true</c> if loaded, <c>false</c> otherwise.</returns>
        public abstract Task<bool> LoadAsync();

        /// <summary>
        /// Creates the Environment.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns><c>true</c> if created/updated, <c>false</c> otherwise.</returns>
        public abstract Task<bool> CreateAsync(EnvironmentStatus status);

        /// <summary>
        /// Logs the progress.
        /// </summary>
        protected void LogProgress(RunProgress progress)
        {
            _progressCallback?.Report(progress);
        }

        /// <summary>
        /// Logs the progress message.
        /// </summary>
        protected void LogProgress(string message)
        {
            LogProgress(new RunProgress(message));
        }
    }
}
