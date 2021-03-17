namespace RestServiceModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public sealed class RestServiceScraper : IDisposable
    {
        const string UrlPattern = @"[^/:]+://(?<host>[^/:]+)(:[^:]+)?$";
        static readonly Regex UrlRegex = new Regex(UrlPattern, RegexOptions.Compiled);
        readonly HttpClient httpClient;
        readonly IList<string> endpoints;


        /// <summary>
        /// Initializes a new instance of the <see cref="RestServiceScraper"/> class.
        /// </summary>
        /// <param name="endpoints">List of endpoints to scrape from. Endpoints must expose metrics in the prometheous format.
        /// Endpoints should be in the form "http://edgeHub:9600/metrics".</param>
        public RestServiceScraper(IList<string> endpoints)
        {
            Preconditions.CheckNotNull(endpoints, nameof(endpoints));

            this.httpClient = new HttpClient(new HttpClientHandler() { UseProxy = false });
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            this.endpoints = endpoints;
            //this.systemTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Scrapes metrics from all endpoints
        /// </summary>
        public Task<IEnumerable<string>> ScrapeEndpointsAsync(CancellationToken cancellationToken)
        {
            return SelectManyAsync(this.endpoints, async endpoint =>
            {
                if (Settings.Current.UseDnsResolution)
                {
                    Logger.Writer.LogInformation($"Trying to get real ip for {endpoint}");
                    endpoint = GetUriWithIpAddress(endpoint);
                    Logger.Writer.LogInformation($"Real ip {endpoint}");
                }

                Logger.Writer.LogInformation($"Scraping endpoint {endpoint}");
                string metricsData = await this.ScrapeEndpoint(endpoint, cancellationToken);

                Logger.Writer.LogInformation($"Scraping finished, response received  from endpoint {endpoint}");
                return metricsData;
            });
        }

        /// <summary>
        /// Taken from LinqEx.cs in Microsoft.Azure.Devices.Edge.Util and modified to not be an extension method. It does not appear to have a specific unit test.
        /// </summary>
        public async Task<IEnumerable<T1>> SelectManyAsync<T, T1>(IEnumerable<T> source, Func<T, Task<T1>> selector)
        {
            return await Task.WhenAll(source.Select(selector));
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        string GetUriWithIpAddress(string endpoint)
        {
            try
            {
                Logger.Writer.LogDebug($"Getting uri with Ip for {endpoint}");
                Match match = UrlRegex.Match(endpoint);
                if (!match.Success)
                {
                    throw new InvalidOperationException($"Endpoint {endpoint} is not a valid URL in the format <protocol>://<host>:<port>/<parameters>");
                }

                var hostGroup = match.Groups["host"];
                string host = hostGroup.Value;
                var ipHostEntry = Dns.GetHostEntry(host);
                var ipAddr = ipHostEntry.AddressList.Length > 0 ? ipHostEntry.AddressList[0].ToString() : string.Empty;
                var builder = new UriBuilder(endpoint);
                builder.Host = ipAddr;
                string endpointWithIp = builder.Uri.ToString();
                Logger.Writer.LogDebug($"Endpoint = {endpoint}, IP Addr = {ipAddr}, Endpoint with Ip = {endpointWithIp}");
                return endpointWithIp;
            }
            catch (Exception ex)
            {
                Logger.Writer.LogError(ex, $"Error ocurred in GetUriWithIpAddress. Error:{ex.Message}");
            }
            return endpoint;
        }

        async Task<string> ScrapeEndpoint(string endpoint, CancellationToken cancellationToken)
        {
            try
            {
                // Temporary. Only needed until edgeHub starts using asp.net to expose endpoints
                //endpoint = this.GetUriWithIpAddress(endpoint);

                HttpResponseMessage result = await this.httpClient.GetAsync(endpoint, cancellationToken);
                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadAsStringAsync();
                }
                else
                {
                    Logger.Writer.LogError($"Error connecting to {endpoint} with result error code {result.StatusCode}");
                }
            }
            catch (System.Net.Sockets.SocketException e) when (e.Source == "System.Net.NameResolution")
            {
                Logger.Writer.LogError($"Error scraping endpoint {endpoint}, hostname likely can not be found - {e}");
            }
            catch (Exception e)
            {
                Logger.Writer.LogError($"Error scraping endpoint {endpoint} - {e}");
                //return ErrorDetails.GetErrorDetails(e, endpoint);
            }

            return string.Empty;
        }
    }
}
