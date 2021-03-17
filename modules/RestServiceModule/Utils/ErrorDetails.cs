namespace RestServiceModule
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Serilog;
    using Serilog.Core;
    using Serilog.Events;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class ErrorDetails
    {
        public string Error { get; set; }
        public string IP { get; set; }
        public string Hostname { get; set; }
        public string Endpoint { get; set; }


        public static string GetErrorDetails(Exception ex, string endpoint)
        {
            ErrorDetails details = new ErrorDetails();
            details.Hostname = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");
            details.Error = ex.ToString();
            details.IP = TryGetIP();
            details.Endpoint = endpoint;
            return JsonConvert.SerializeObject(details);
        }

        static string TryGetIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                List<string> ips = new List<string>();

                foreach (var ip in host.AddressList)
                {
                    ips.Add(ip.ToString());
                }

                return string.Join(";", ips);
            }
            catch (Exception)
            {
                return string.Empty;

            }

        }

    }
}
