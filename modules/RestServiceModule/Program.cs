namespace RestServiceModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    using System.Diagnostics;
    internal static class Program
    {

        private static void WaitForDebugger()
        {
            Logger.Writer.LogInformation("Waiting for debugger to attach");
            for (int i = 0; i < 300 && !Debugger.IsAttached; i++)
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(250);
        }

        public static int Main() => MainAsync().Result;

        static async Task<int> MainAsync()
        {
            (CancellationTokenSource cts, ManualResetEventSlim completed, Option<object> handler) = ShutdownHandler.Init(TimeSpan.FromSeconds(5), Logger.Writer);

            // wait up to 30 seconds for debugger to attach if in a debug build
#if DEBUG
            WaitForDebugger();
#endif

            Logger.Writer.LogInformation($"Starting Rest Service Caller Module with the following settings:\r\n{Settings.Current}");

            //ITransportSettings transport = ProtocolHelper.GetTransportSettings(Settings.Current.Protocol); // new MqttTransportSettings(TransportType.Mqtt);
            
            MqttTransportSettings transport = new MqttTransportSettings (TransportType.Mqtt_WebSocket_Only);

            ITransportSettings[] transportSettings = { transport };
            ModuleClient moduleClient = null;
            try
            {
                moduleClient = await ModuleClient.CreateFromEnvironmentAsync(transportSettings);

                RestServiceScraper scraper = new RestServiceScraper(Settings.Current.Endpoints);
                RestServiceResultPublisher publisher;
                publisher = new RestServiceResultPublisher(moduleClient);

                using (RestServiceScraperAndUpload metricsScrapeAndUpload = new RestServiceScraperAndUpload(scraper, publisher))
                {
                    TimeSpan scrapeAndUploadInterval = TimeSpan.FromSeconds(Settings.Current.CallFrequencySecs);
                    metricsScrapeAndUpload.Start(scrapeAndUploadInterval);
                    // await cts.Token.WhenCanceled();
                    WaitHandle.WaitAny(new WaitHandle[] { cts.Token.WaitHandle });
                }
            }
            catch (Exception e)
            {
                Logger.Writer.LogError(e, "Error occurred during metrics collection setup.");
            }
            finally
            {
                moduleClient?.Dispose();
            }

            completed.Set();
            handler.ForEach(h => GC.KeepAlive(h));

            Logger.Writer.LogInformation("Rest Service Caller Main() finished.");
            return 0;
        }

        // static void Main(string[] args)
        // {
        //     Init().Wait();

        //     // Wait until the app unloads or is cancelled
        //     var cts = new CancellationTokenSource();
        //     AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
        //     Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
        //     WhenCancelled(cts.Token).Wait();
        // }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        // public static Task WhenCancelled(CancellationToken cancellationToken)
        // {
        //     var tcs = new TaskCompletionSource<bool>();
        //     cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
        //     return tcs.Task;
        // }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        // static async Task Init()
        // {
        //     MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
        //     ITransportSettings[] settings = { mqttSetting };

        //     // Open a connection to the Edge runtime
        //     ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
        //     await ioTHubModuleClient.OpenAsync();
        //     Console.WriteLine("IoT Hub module client initialized.");

        //     // Register callback to be called when a message is received by the module
        //     await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", PipeMessage, ioTHubModuleClient);
        // }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        // static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        // {
        //     int counterValue = Interlocked.Increment(ref counter);

        //     var moduleClient = userContext as ModuleClient;
        //     if (moduleClient == null)
        //     {
        //         throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
        //     }

        //     byte[] messageBytes = message.GetBytes();
        //     string messageString = Encoding.UTF8.GetString(messageBytes);
        //     Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

        //     if (!string.IsNullOrEmpty(messageString))
        //     {
        //         using (var pipeMessage = new Message(messageBytes))
        //         {
        //             foreach (var prop in message.Properties)
        //             {
        //                 pipeMessage.Properties.Add(prop.Key, prop.Value);
        //             }
        //             await moduleClient.SendEventAsync("output1", pipeMessage);

        //             Console.WriteLine("Received message sent");
        //         }
        //     }
        //     return MessageResponse.Completed;
        // }
    }
}
