using Amuse.App.Services;
using Amuse.Common;
using Amuse.Common.Config;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Amuse.App.Runtime
{
    /// <summary>
    /// PipelineClient implemntation for Amuse.Host.StableDiffusionCpp
    /// Implements the <see cref="Amuse.App.Runtime.BackendClient" />
    /// </summary>
    /// <seealso cref="Amuse.App.Runtime.BackendClient" />
    public sealed class StableDiffusionCppClient : BackendClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionCppClient"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="mediaService">The media service.</param>
        /// <param name="logger">The logger.</param>
        public StableDiffusionCppClient(Settings settings, IMediaService mediaService, ILogger logger)
            : base(settings, mediaService, logger)
        {
            StopHostOnException = true;
        }


        /// <summary>
        /// Create PipelineClient targeting Amuse.Host.StableDiffusionCpp.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.OperationCanceledException"></exception>
        protected override async Task<PipelineClient> CreatePipelineClientAsync(CancellationToken cancellationToken = default)
        {
            var createOptions = new PipelineCreateOptions
            {
                ServerPort = 2345,
                ServerAddress = "127.0.0.1",
                Directory = Path.Combine(App.DirectoryData, "Backend")
            };
            var clientConfig = new ClientConfig
            {
                ServerPath = App.DirectoryServer,
                ServerType = ServerType.StableDiffusionCpp,
                IsDebugMode = Settings.IsServerDebugEnabled,
            };
            return await CreatePipelineClientAsync(clientConfig, createOptions, cancellationToken);
        }

    }
}
