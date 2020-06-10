// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SampleSender
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;
    using Newtonsoft.Json;

    public class Program
    {
        private static EventHubClient eventHubClient;
        private const string EventHubConnectionString = "Endpoint=sb://ehdriverinfo.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=ATgAlpSCvmSwR0S0M+rO/Y6iy4OJo8ycD6B3jMbct6E=";
        private const string EventHubName = "deviceinfo";
        private static bool SetRandomPartitionKey = false;

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            // Creates an EventHubsConnectionStringBuilder object from a the connection string, and sets the EntityPath.
            // Typically the connection string should have the Entity Path in it, but for the sake of this simple scenario
            // we are using the connection string from the namespace.
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(EventHubConnectionString)
            {
                EntityPath = EventHubName
            };

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            await PostTelemetryAsync(10);

            //await SendMessagesToEventHub(100);

            //await eventHubClient.CloseAsync();

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }

        // Creates an Event Hub client and sends 100 messages to the event hub.
        private static async Task SendMessagesToEventHub(int numMessagesToSend)
        {
            //var rnd = new Random();

            for (var i = 0; i < numMessagesToSend; i++)
            {
                try
                {
                    var message = $"Message {i}";

                    // Set random partition key?
                    if (SetRandomPartitionKey)
                    {
                        var pKey = Guid.NewGuid().ToString();
                        await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)), pKey);
                        Console.WriteLine($"Sent message: '{message}' Partition Key: '{pKey}'");
                    }
                    else
                    {
                        await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
                        Console.WriteLine($"Sent message: '{message}'");
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"{DateTime.Now} > Exception: {exception.Message}");
                }

                await Task.Delay(10);
            }

            Console.WriteLine($"{numMessagesToSend} messages sent.");
        }

        private static Task<HttpResponseMessage> PostTelemetryAsync(int count)
        {
            // Use Event Hubs Signature Generator 0.2.0.1 to generate the token
            var sas = "SharedAccessSignature sr=ATgAlpSCvmSwR0S0M+rO/Y6iy4OJo8ycD6B3jMbct6E=";

            // Namespace info.
            var serviceNamespace = "ehDriverInfo";
            var hubName = "deviceinfo";
            var url = string.Format("{0}/publishers/{1}/messages", hubName, 1001);
            Task<HttpResponseMessage> returnTask = null;

            // Create client.
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(string.Format("https://{0}.servicebus.windows.net/", serviceNamespace))
            };

            for (int i = 0; i < count; i++)
            {
                //var messageInfo = new { id = i, temperature = 70 };                
                var messageInfo = new
                {
                    v = 1,
                    t = "event",
                     cid = "C9A57091-81E1-4FDB-BBFC-D10D29ACE142",
                    aip = true,
                    tid = "UA-41423377-13",
                    ul = "en",
                    av = "6.3.37b4",
                    an = "Driver",
                    ec = "HardwareActions",
                    ea = "Use transducer tip",
                    ev = 3,
                    el = "5C~1063A0",
                    cd1 = "Cintiq%20Pro%2032%20%28DTH-3220%29"
                };

                var payload = JsonConvert.SerializeObject(messageInfo);

                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", sas);

                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                //content.Headers.Add("ContentType", DeviceTelemetry.ContentType);

                returnTask = httpClient.PostAsync(url, content);
            }
            return returnTask;
        }

    }
}
