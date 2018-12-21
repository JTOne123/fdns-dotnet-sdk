using System;
using System.Collections.Generic;
using System.Net;

namespace Foundation.Sdk
{
    /// <summary>
    /// Class representing a result from calling one of the Foundation Services
    /// </summary>
    public sealed class ServiceResult<T>
    {
        public string URI { get; } = string.Empty;
        public T Value { get; }
        public string ServiceName { get; } = string.Empty;
        public bool IsSuccess 
        {
            get 
            {
                int code = (int) Code;
                if (code >= 200 && code <= 299) return true;
                else return false;
            }
        }
        public HttpStatusCode Code { get; internal set; }
        public TimeSpan Elapsed { get; }
        public string CorrelationId { get; }
        public string Message { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;

        public ServiceResult(string uri, TimeSpan elapsed, T value, string serviceName, HttpStatusCode code, string correlationId, string message = "")
        {
            #region Input Validation
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentNullException(nameof(uri));
            }
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }
            if (correlationId == null)
            {
                throw new ArgumentNullException(nameof(correlationId));
            }
            if (elapsed != null && elapsed.TotalMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            #endregion // Input Validation

            URI = uri;
            Value = value;
            ServiceName = serviceName;
            Code = code;
            Elapsed = elapsed;
            CorrelationId = correlationId;
            Message = message;
        }
    }
}