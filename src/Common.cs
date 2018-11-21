using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Foundation.Sdk
{
    public static class Common
    {
        public const string MEDIA_TYPE_APPLICATION_JSON = "application/json";
        public const string MEDIA_TYPE_TEXT_PLAIN = "text/plain";
        public const string OBJECT_SERVICE_NAME = "Object";
        public const string STORAGE_SERVICE_NAME = "Storage";
        public const string INDEXING_SERVICE_NAME = "Indexing";
        public const string RULES_SERVICE_NAME = "Rules";
        public const string READ_AUTHORIZATION_NAME = "read";
        public const string INSERT_AUTHORIZATION_NAME = "insert";
        public const string UPDATE_AUTHORIZATION_NAME = "update";
        public const string DELETE_AUTHORIZATION_NAME = "delete";

        public static async Task<ServiceResult<T>> GetHttpResultAsServiceResultAsync<T>(HttpResponseMessage response, string serviceName, string uri, Dictionary<string, string> headers)
        {
            var sw = new Stopwatch();
            sw.Start();

            var responseValue = await response.Content.ReadAsStringAsync();

            sw.Stop();

            T objectValue = default(T);

            if (typeof(T) == typeof(String))
            {
                if (responseValue != null)
                {
                    objectValue = (T)(object)responseValue;
                }
                else
                {
                    objectValue = (T)(object)string.Empty;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(responseValue))
                {
                    objectValue = JsonConvert.DeserializeObject<T>(responseValue);
                }
            }

            string message = string.Empty;

            if (response.IsSuccessStatusCode == false && !string.IsNullOrEmpty(responseValue))
            {
                var errorPayload = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseValue);
                if (errorPayload.ContainsKey("message"))
                {
                    message = errorPayload["message"] != null ? errorPayload["message"].ToString() : string.Empty;
                }
            }

            string correlationId = GetCorrelationIdFromHeaders(headers);
            var result = new ServiceResult<T>(uri, sw.Elapsed, objectValue, serviceName, response.IsSuccessStatusCode, response.StatusCode, correlationId, message);
            return result;
        }

        public static string GetCorrelationIdFromHeaders(Dictionary<string, string> headers) => headers != null ? (headers.ContainsKey("X-Correlation-Id") ? headers["X-Correlation-Id"] : string.Empty) : string.Empty;

        public static void AddHttpRequestHeaders(HttpRequestMessage requestMessage, string senderName, string destinationName, Dictionary<string, string> headers)
        {
            requestMessage.Headers.Add("X-Correlation-From", $"{senderName}");
            requestMessage.Headers.Add("X-Correlation-To", $"{destinationName}");

            if (headers != null)
            {
                foreach (var kvp in headers)
                {
                    requestMessage.Headers.Add(kvp.Key, kvp.Value);
                }

                if (!headers.ContainsKey("X-Correlation-Id"))
                {
                    string correlationId = System.Guid.NewGuid().ToString().Substring(0, 18);
                    requestMessage.Headers.Add("X-Correlation-Id", correlationId);
                }
            }
        }

        /// <summary>
        /// Gets a config value for a variable name, preferring ENV variables over appsettings variables when both are present
        /// </summary>
        /// <param name="configuration">The config object to use for pulling keys and values</param>
        /// <param name="environmentVariableName">The name of the variable to use for getting the config value</param>
        /// <param name="appSettingsVariableName">The name of the appsettings variable to use for getting the config value</param>
        /// <param name="defaultValue">The default value to use (if any) if neither config location has a value for this variable</param>
        /// <returns>string representing the config value</returns>
        public static string GetConfigurationVariable(IConfiguration configuration, string environmentVariableName, string appSettingsVariableName, string defaultValue = "")
        {
            string variableValue = string.Empty;
            if (!string.IsNullOrEmpty(appSettingsVariableName) && !string.IsNullOrEmpty(configuration[appSettingsVariableName]))
            {
                variableValue = configuration[appSettingsVariableName];
            }
            if (!string.IsNullOrEmpty(environmentVariableName) && !string.IsNullOrEmpty(configuration[environmentVariableName]))
            {
                variableValue = configuration[environmentVariableName];
            }

            if (string.IsNullOrEmpty(variableValue) && !string.IsNullOrEmpty(defaultValue))
            {
                variableValue = defaultValue;
            }

            return variableValue;
        }

        public static string GetLogPrefix(string serviceName, string correlationId) => $"{correlationId}: Called {serviceName}";

        public static Dictionary<string, string> NormalizeHeaders(Dictionary<string, string> headers) => headers != null ? headers : new Dictionary<string, string>();
    }
}