// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace RestServiceModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class RestServiceResultPublisher
    {
        readonly ModuleClient moduleClient;

        public RestServiceResultPublisher(ModuleClient moduleClient)
        {
            this.moduleClient = Preconditions.CheckNotNull(moduleClient, nameof(moduleClient));
        }

        public async Task<bool> PublishAsync(IEnumerable<string> responses, CancellationToken cancellationToken)
        {
            try
            {
                Preconditions.CheckNotNull(responses, nameof(responses));
            
                foreach (var messageJson in responses)
                {
                    if(string.IsNullOrEmpty(messageJson))
                    {
                        Logger.Writer.LogInformation("MessageJson is empty, it won't be sent");
                        continue;
                    }
                    byte[] data = Encoding.UTF8.GetBytes(messageJson);
                    Message message = new Message(data);
                    await this.moduleClient.SendEventAsync("restOutput", message);
                    Logger.Writer.LogInformation("Successfully sent rest service responses to IoT Hub");
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Writer.LogError(e, "Error uploading responses to IoTHub");
                return false;
            }
        }
    }
}
