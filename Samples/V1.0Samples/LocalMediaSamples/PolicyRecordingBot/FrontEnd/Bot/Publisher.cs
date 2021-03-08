// <copyright file="Publisher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.PolicyRecordingBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Newtonsoft.Json;

    /// <summary>Publish messages to external server.</summary>
    public class Publisher
    {
        private const string URL = "http://teams.featherinthecap.com/publish";
        private static HttpClient httpClient = new HttpClient();

        /// <summary>Publish a message.</summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void Publish(string key, string value)
        {
            Console.WriteLine($"[{key}] {value}");
            Post(key, value);
        }

        /// <summary>Publish a message.</summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void Publish(string key, object value)
        {
            try
            {
                string x = JsonConvert.SerializeObject(value);
                Console.WriteLine($"[{key}] {x}");
                Post(key, x);
            }
            catch (Exception ex)
            {
                Post("ERROR", ex.ToString());
            }
        }

        /// <summary>Send a post request.</summary>
        /// <param name="key">key.</param>
        /// <param name="value">value.</param>
        private static void Post(string key, string value)
        {
            var values = new Dictionary<string, string> { { key, value } };
            var content = new FormUrlEncodedContent(values);
            httpClient.PostAsync(URL, content);
        }
    }
}
