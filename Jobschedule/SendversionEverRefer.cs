using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Jobschedule
{
    public class SendversionEverRefer
    {
     
        static async Task Sendversion()
        {
            string username = "10829-gw2";
            string password = "LTsIb53Wv00f1oLe056J";

            // Step 1: Login and retrieve token
            string token = await Login(username, password);
            Console.WriteLine("Token: " + token);

            // Step 2: Send heartbeat with token
            await SendHeartbeat(token);
        }

        private static async Task<string> Login(string username, string password)
        {
            var loginUrl = "https://sync-api.hie-rayong.everapp.io/v2/user/login";
            var loginData = new
            {
                username,
                password
            };

            var json = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpClient client = new HttpClient();
            var response = await client.PostAsync(loginUrl, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            dynamic responseObject = JsonConvert.DeserializeObject(responseString);

            return responseObject.token;
        }

        private static async Task SendHeartbeat(string token)
        {
            var heartbeatUrl = "https://sync-api.hie-rayong.everapp.io/v2/user/heartbeat";
            var heartbeatData = new
            {
                GatewayVersion = "1.0.0"
            };

            var json = JsonConvert.SerializeObject(heartbeatData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync(heartbeatUrl, content);
            response.EnsureSuccessStatusCode();

            Console.WriteLine("Heartbeat sent successfully!");
        }

    }
}
