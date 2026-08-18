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
            return await SendRequestAsync<CapabilitiesModel>(() => _httpClient.GetAsync(endpoint, cancellationToken), cancellationToken);
        }


        /// <summary>
        /// Get existing job
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<JobModel> GetJobAsync(JobModel job, CancellationToken cancellationToken = default)
        {
            const string endpoint = "sdcpp/v1/jobs/{0}";
            return await SendRequestAsync<JobModel>(() => _httpClient.GetAsync(string.Format(endpoint, job.Id), cancellationToken), cancellationToken);
        }


        /// <summary>
        /// Create new Image job
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<JobModel> CreateJobAsync(ImageParams parameters, CancellationToken cancellationToken = default)
        {
            const string endpoint = "sdcpp/v1/img_gen";
            return await SendRequestAsync<JobModel>(() => _httpClient.PostAsJsonAsync(endpoint, parameters, _serializerOptions, cancellationToken), cancellationToken);
        }


        /// <summary>
        /// Create new Video job
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<JobModel> CreateJobAsync(VideoParams parameters, CancellationToken cancellationToken = default)
        {
            const string endpoint = "sdcpp/v1/vid_gen";
            return await SendRequestAsync<JobModel>(() => _httpClient.PostAsJsonAsync(endpoint, parameters, _serializerOptions, cancellationToken), cancellationToken);
        }


        /// <summary>
        /// Cancel the specified job
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<bool> CancelJobAsync(JobModel job)
        {
            try
            {
                const string endpoint = "sdcpp/v1/jobs/{0}/cancel";
                using (var response = await _httpClient.PostAsync(string.Format(endpoint, job.Id), content: null))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                        return false; // Job cannot be canceled

                    response.EnsureSuccessStatusCode();
                    return true;
                }
            }
            catch (HttpRequestException ex)
            {
                throw new StableDiffusionApiException($"Failed to communicate with StableDiffusion.cpp to cancel job '{job.Id}'.", ex);
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }


        /// <summary>
        /// Send API request request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestFunc">The request function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="Amuse.Host.StableDiffusionCpp.StableDiffusionApiException">StableDiffusion.cpp responded with an empty or invalid payload.</exception>
        /// <exception cref="Amuse.Host.StableDiffusionCpp.StableDiffusionApiException">StableDiffusion.cpp responded with error: {ex.Message}</exception>
        /// <exception cref="Amuse.Host.StableDiffusionCpp.StableDiffusionApiException">Failed to parse StableDiffusion.cpp response.</exception>
        private async Task<T> SendRequestAsync<T>(Func<Task<HttpResponseMessage>> requestFunc, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await requestFunc();
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<T>(_serializerOptions, cancellationToken);
                if (result == null)
                    throw new StableDiffusionApiException("StableDiffusion.cpp responded with an invalid payload.");

                return result;
            }
            catch (HttpRequestException ex)
            {
                throw new StableDiffusionApiException($"StableDiffusion.cpp responded with error: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new StableDiffusionApiException("Failed to parse StableDiffusion.cpp response.", ex);
            }
            finally
            {
                response?.Dispose();
            }
        }

    }
}