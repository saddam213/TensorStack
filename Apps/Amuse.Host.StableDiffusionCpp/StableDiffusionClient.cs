using Amuse.Host.StableDiffusionCpp.Common;
using Amuse.Host.StableDiffusionCpp.Config;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Amuse.Host.StableDiffusionCpp
{
    internal sealed class StableDiffusionClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ServerConfig _configuration;
        private readonly JsonSerializerOptions _serializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionClient"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public StableDiffusionClient(ServerConfig configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_configuration.BaseUrl)
            };
            _serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }


        /// <summary>
        /// Get model capabilities
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<CapabilitiesModel> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            const string endpoint = "sdcpp/v1/capabilities";
            using (var response = await _httpClient.GetAsync(endpoint, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CapabilitiesModel>(_serializerOptions, cancellationToken: cancellationToken);
            }
        }


        /// <summary>
        /// Get existing job
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<JobModel> GetJobAsync(JobModel job, CancellationToken cancellationToken = default)
        {
            const string endpoint = "sdcpp/v1/jobs/{0}";
            using (var response = await _httpClient.GetAsync(string.Format(endpoint, job.Id), cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<JobModel>(_serializerOptions, cancellationToken: cancellationToken);
            }
        }


        /// <summary>
        /// Create new Image job
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<JobModel> CreateJobAsync(ImageParams parameters, CancellationToken cancellationToken = default)
        {
            const string endpoint = "sdcpp/v1/img_gen";
            using (var response = await _httpClient.PostAsJsonAsync(endpoint, parameters, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<JobModel>(_serializerOptions, cancellationToken: cancellationToken);
            }
        }


        /// <summary>
        /// Create new Video job
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<JobModel> CreateJobAsync(VideoParams parameters, CancellationToken cancellationToken = default)
        {
            const string endpoint = "sdcpp/v1/vid_gen";
            using (var response = await _httpClient.PostAsJsonAsync(endpoint, parameters, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<JobModel>(_serializerOptions, cancellationToken: cancellationToken);

            }
        }


        /// <summary>
        /// Cancel the specified job
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task CancelJobAsync(JobModel job, CancellationToken cancellationToken = default)
        {
            const string endpoint = "sdcpp/v1/jobs/{0}/cancel";
            using (var response = await _httpClient.GetAsync(string.Format(endpoint, job.Id), cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }

    }
}
