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
    using System.Web;
    using System.Globalization;
    using System.Security.Cryptography;

    public class Program
    {
        private static EventHubClient eventHubClient;
        private const string EventHubConnectionString = "Endpoint=sb://ehdriverinfo.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=ATgAlpSCvmSwR0S0M+rO/Y6iy4OJo8ycD6B3jMbct6E=";
        private const string EventHubName = "deviceinfo";
        private const string TestEventHubName = "eventhubpost";
        private static bool SetRandomPartitionKey = false;

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {        

            await SendDataUsingHttp();
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }

        /// <summary>
        /// Push data into event hub through Http POST
        /// </summary>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> SendDataUsingHttp()
        {

            // Namespace info.
            var serviceNamespace = "ehdriverinfo";
            var hubName = "eventhubpost";
            var url = string.Format("{0}/publishers/{1}/messages", hubName, 1);
            //var url = string.Format("{0}/messages", hubName);
            var baseUri = new
             Uri(string.Format("https://{0}.servicebus.windows.net/"
                          , serviceNamespace));
            var SharedAccessKeyName = "RootManageSharedAccessKey";
            var SharedAccessKey = "ATgAlpSCvmSwR0S0M+rO/Y6iy4OJo8ycD6B3jMbct6E=";
            var sasToken =
            createSasToken(baseUri.ToString(), SharedAccessKeyName,
                           SharedAccessKey);
            var evtData = new
            {
                Temperature = new Random().Next(20, 50)
            };

            var payload = JsonConvert.SerializeObject(evtData);
            // Create client.
            var httpClient = new HttpClient
            {
                BaseAddress = baseUri
            };


            httpClient.DefaultRequestHeaders.Accept.Clear();     
            bool isAuthAdded = httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", sasToken);

            var content = new StringContent(payload, Encoding.UTF8);
            content.Headers.ContentType = new
              System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage result = null;
            try
            {
                result = await httpClient.PostAsync(url, content);
                Console.WriteLine(result.StatusCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }

       
        /// <summary>
        /// Create SAS token in the format required by EH
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="keyName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string createSasToken(string baseUri, string keyName, string key)
        {
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var week = 60 * 60 * 24 * 7;
            var expiration = Convert.ToString((int)sinceEpoch.TotalSeconds +
            week);
            string stringToSign = HttpUtility.UrlEncode(baseUri) + "\n" +
            expiration;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)); //--
            var signature =
            Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(
            stringToSign)));
            var sasToken = String.Format(CultureInfo.InvariantCulture,
                   "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}",
            HttpUtility.UrlEncode(baseUri), HttpUtility.UrlEncode(signature),
            expiration, keyName);
            return sasToken;
        }

    }
}
