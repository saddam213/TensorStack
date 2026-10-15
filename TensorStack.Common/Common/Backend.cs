using System;
using System.IO;
using System.Threading.Tasks;
using TensorStack.Common.Pipeline;

namespace TensorStack.Common
{
    public abstract class Backend<T> : IDisposable where T : IRunProgress
    {
        private readonly BackendConfig _config;
        private readonly IProgress<T> _progressCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="Backend{T}"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="progressCallback">The progress callback.</param>
        protected Backend(BackendConfig config, IProgress<T> progressCallback = null)
        {
            _config = config;
            _progressCallback = progressCallback;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Backend{T}"/> class.
        /// </summary>
        /// <param name="vendor">The vendor.</param>
        /// <param name="progressCallback">The progress callback.</param>
        protected Backend(VendorType vendor, IProgress<T> progressCallback = null)
        {
            _config = GetVendorConfig(vendor);
            _progressCallback = progressCallback;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public BackendConfig Config => _config;

        /// <summary>
        /// Gets the backend.
        /// </summary>
        public abstract BackendType Type { get; }

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
        /// Setup the Environment.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns><c>true</c> if created/updated, <c>false</c> otherwise.</returns>
        public abstract Task<bool> SetupAsync(BackendStatus status);

        /// <summary>
        /// Gets the vendor configuration.
        /// </summary>
        /// <param name="vendor">The vendor.</param>
        protected abstract BackendConfig GetVendorConfig(VendorType vendor);

        /// <summary>
        /// Gets the directory.
        /// </summary>
        public virtual string GetDirectory() => Path.Combine(_config.Directory, _config.Name);

        /// <summary>
        /// Initialize an environment, Load if exists, Update if Load fails, else create new.
        /// </summary>
        /// <returns><c>true</c> if loaded, <c>false</c> otherwise.</returns>
        public virtual async Task<bool> InitializeAsync()
        {
            if (Exists())
            {
                if (await LoadAsync())
                    return true;

                if (await SetupAsync(BackendStatus.Update) && await LoadAsync())
                    return true;
            }

            if (await SetupAsync(BackendStatus.Create) && await LoadAsync())
                return true;

            return false;
        }


        /// <summary>
        /// Logs the progress message.
        /// </summary>
        protected abstract void LogProgress(string message);


        /// <summary>
        /// Logs the progress.
        /// </summary>
        protected virtual void LogProgress(T progress)
        {
            _progressCallback?.Report(progress);
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Releases the unmanaged resources used by the Backend and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) { }
    }


    /// <summary>
    /// Gets the Vendor default configuration.
    /// </summary>
    /// <param name="vendor">The vendor.</param>
    public interface IVendorBackend
    {
        static abstract BackendConfig GetConfig(VendorType vendor);
    }
}
