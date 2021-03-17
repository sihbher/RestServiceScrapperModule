namespace RestServiceModule
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    class RestServiceScraperAndUpload : IDisposable
    {
        private readonly RestServiceScraper scraper;
        private readonly RestServiceResultPublisher publisher;
        private PeriodicTask periodicScrapeAndUpload;

        public RestServiceScraperAndUpload(RestServiceScraper scraper, RestServiceResultPublisher publisher)
        {
            this.scraper = Preconditions.CheckNotNull(scraper);
            this.publisher = Preconditions.CheckNotNull(publisher);
        }

        public void Start(TimeSpan scrapeAndUploadInterval)
        {
            this.periodicScrapeAndUpload = new PeriodicTask(this.ScrapeAndUploadMetricsAsync, scrapeAndUploadInterval, scrapeAndUploadInterval, Logger.Writer, "Scrape and Upload Rest endpoints", instantStart: true);
        }

        public void Dispose()
        {
           this.periodicScrapeAndUpload?.Dispose();
        }

        async Task ScrapeAndUploadMetricsAsync(CancellationToken cancellationToken)
        {
            try
            {
                IEnumerable<string> responses = await this.scraper.ScrapeEndpointsAsync(cancellationToken);

                await this.publisher.PublishAsync(responses, cancellationToken);
            }
            catch (Exception e)
            {
                Logger.Writer.LogError(e, "Error scraping and uploading Rest endpoints");
            }
        }
    }
}
