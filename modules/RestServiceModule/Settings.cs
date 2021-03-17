namespace RestServiceModule
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    internal class Settings
    {
        public static Settings Current = Create();


        public int CallFrequencySecs { get; }
        public List<string> Endpoints { get; }
        public bool UseDnsResolution { get; }
        public string Protocol {get;}

        private Settings(string endpoints, int callFrequencySecs, bool useDns, string protocol)
        {
            this.CallFrequencySecs = Preconditions.CheckRange(callFrequencySecs, 1);


            this.Endpoints = new List<string>();
            foreach (string endpoint in endpoints.Split(","))
            {
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    this.Endpoints.Add(endpoint);
                }
            }

            if (this.Endpoints.Count == 0)
            {
                Logger.Writer.LogError("No scraping endpoints specified, exiting");
                throw new ArgumentException("No endpoints specified for which to scrape");
            }

            this.UseDnsResolution = useDns;
        }

        private static Settings Create()
        {
            try
            {
                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddEnvironmentVariables()
                    .Build();

                return new Settings(
                    configuration.GetValue<string>("Endpoints"),
                    configuration.GetValue<int>("CallFrequencySecs", 60),
                    configuration.GetValue<bool>("UseDnsResolution", false),
                    configuration.GetValue<string>("Protocol", "amqpws")
                    );
            }
            catch (ArgumentException e)
            {
                Logger.Writer.LogCritical("Error reading arguments from environment variables. Make sure all required parameter are present");
                Logger.Writer.LogCritical(e.ToString());
                Environment.Exit(2);
                throw new Exception();  // to make code analyzers happy (this line will never run)
            }
        }



        // TODO: is this used anywhere important? Make sure to test it if so
        public override string ToString()
        {
            string HostName = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");
            Console.WriteLine($"IOTEDGE_GATEWAYHOSTNAME: {HostName}");

            var fields = new Dictionary<string, string>()
            {
                { nameof(this.CallFrequencySecs), this.CallFrequencySecs.ToString() },
                 { nameof(this.UseDnsResolution), this.UseDnsResolution.ToString() },
                { nameof(this.Endpoints), JsonConvert.SerializeObject(this.Endpoints, Formatting.Indented) }
            };

            return $"Settings:{Environment.NewLine}{string.Join(Environment.NewLine, fields.Select(f => $"{f.Key}={f.Value}"))}";
        }
    }
}
