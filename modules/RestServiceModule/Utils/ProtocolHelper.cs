


namespace RestServiceModule
{
    using System;
    using Microsoft.Azure.Devices.Client;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;

    public static class ProtocolHelper
    {

        public static ITransportSettings GetTransportSettings(string protocol)
        {
            TransportType ttype = GetProtocol(protocol);
            if(ttype == TransportType.Amqp  || ttype == TransportType.Amqp_WebSocket_Only ||ttype == TransportType.Amqp_Tcp_Only)
            {
                    return new AmqpTransportSettings(ttype);
            }
            else if(ttype == TransportType.Mqtt || ttype == TransportType.Mqtt_Tcp_Only ||ttype == TransportType.Mqtt_WebSocket_Only)
            {
                 return new MqttTransportSettings(ttype);
            }
            
                return new Http1TransportSettings();
        }

        //Get the protocol from the string parameter
        private static TransportType GetProtocol(string protocol)
        {
            if (string.IsNullOrEmpty(protocol))
            {
                Console.WriteLine("Protocol not found. Mqtt will be used");
                return TransportType.Mqtt;
            }

            protocol = protocol.ToLower();
            TransportType realProtocol = TransportType.Amqp_WebSocket_Only;
            
            //Firs we try to get from the exact name
            // var enumNames = Enum.GetNames(typeof(TransportType));
            // string correctName = enumNames.SingleOrDefault(x => x.ToLower().Equals(protocol));
            
            // if (!string.IsNullOrEmpty(correctName)
            //  && Enum.TryParse<TransportType>(correctName, true, out realProtocol))
            // {
            //     Console.WriteLine($"Protocol found!. {realProtocol} will be used");
            //     return realProtocol;
            // }

            //from know variations
            switch (protocol)
            {
                case "amqp_tcp_only":
                case "amqps":
                    realProtocol = TransportType.Amqp_Tcp_Only;
                    break;
                case "amqp":
                    realProtocol = TransportType.Amqp;
                    break;
                case "amqp_websocket_only":
                case "amqp_ws":
                case "amqpws":
                    realProtocol = TransportType.Amqp_WebSocket_Only;
                    break;

                case "mqtt":
                    realProtocol = TransportType.Mqtt;
                    break;
                case "mqtt_tcp_only":
                case "mqtts":
                    realProtocol = TransportType.Mqtt_Tcp_Only;
                    break;
                case "mqtt_websocket_only":
                case "mqtt_ws":
                case "mqttws":
                    realProtocol = TransportType.Mqtt_WebSocket_Only;
                    break;


                case "http":
                case "http1":
                case "https":
                    realProtocol = TransportType.Http1;
                    break;


                default:
                    Console.WriteLine($"Protocol not found: {protocol}, mqtt wil be used");
                    realProtocol = TransportType.Amqp_WebSocket_Only;
                    break;

            }
            return realProtocol;
        }
    }
}