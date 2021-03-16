// Microsoft.Graph.Communications.Client.CommunicationsClientExtensions
// Version=1.2.0.850

using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Core.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Graph.Communications.Client {
    public static class CommunicationsClientExtensions {
        public static async Task<HttpResponseMessage> ProcessNotificationAsync(
            this ICommunicationsClient client,
            HttpRequestMessage request) {
            client.NotNull<ICommunicationsClient>(nameof(client));
            request.NotNull<HttpRequestMessage>(nameof(request));
            Stopwatch stopwatch = Stopwatch.StartNew();
            Guid scenarioId = client.GraphLogger.ParseScenarioId(request);
            Guid requestId = client.GraphLogger.ParseRequestId(request);
            client.GraphLogger.Log(TraceLevel.Info, string.Format("Authenticating inbound request ({0}): {1}", (object) requestId, (object) request.RequestUri), memberName: nameof(ProcessNotificationAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientExtensions.cs", lineNumber: 49);
            CommsNotifications notifications = (CommsNotifications) null;
            try {
                notifications = NotificationProcessor.ExtractNotifications(await request.Content.ReadAsStringAsync().ConfigureAwait(false), client.Serializer);
            }
            catch (Microsoft.Graph.ServiceException ex)
            {
                return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, ex.StatusCode >= HttpStatusCode.OK ? ex.StatusCode : HttpStatusCode.BadRequest, stopwatch, (Exception) ex);
            }
      catch (Exception ex)
            {
                return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, HttpStatusCode.BadRequest, stopwatch, ex);
            }
            RequestValidationResult validationResult;
            try {
                validationResult = await client.AuthenticationProvider.ValidateInboundRequestAsync(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                client.GraphLogger.Error(ex, string.Format("Failed authenticating inbound request ({0}): {1}", (object) requestId, (object) request.RequestUri), memberName: nameof(ProcessNotificationAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientExtensions.cs", lineNumber: 81);
                Microsoft.Graph.Communications.Core.Exceptions.ClientException clientException = new Microsoft.Graph.Communications.Core.Exceptions.ClientException(new Microsoft.Graph.Error()
        {
                        Code = "clientCallbackError",
                        Message = "Unexpected exception happened on client when authenticating request."
                    }, ex);
                int num = (int) client.GraphLogger.LogHttpRequest(request, HttpStatusCode.InternalServerError, (object) notifications, (Exception) clientException, nameof(ProcessNotificationAsync), "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientExtensions.cs", 93);
                throw clientException;
            }
            if (!validationResult.IsValid) {
                client.GraphLogger.Log(TraceLevel.Warning, "Client returned failed token validation", memberName: nameof(ProcessNotificationAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientExtensions.cs", lineNumber: 99);
                return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, HttpStatusCode.Unauthorized, stopwatch);
            }
            try {
                Dictionary < string, object > dictionary = request.GetHttpAndContentHeaders().ToDictionary<KeyValuePair<string, IEnumerable<string>>, string, object>((Func<KeyValuePair<string, IEnumerable<string>>, string>)(pair => pair.Key), (Func<KeyValuePair<string, IEnumerable<string>>, object>)(pair => (object) string.Join(",", pair.Value)), (IEqualityComparer<string>) StringComparer.OrdinalIgnoreCase);
                client.ProcessNotifications(request.RequestUri, notifications, validationResult.TenantId, requestId, scenarioId, (IDictionary<string, object>) dictionary);
            }
            catch (Microsoft.Graph.ServiceException ex)
            {
                return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, ex.StatusCode >= HttpStatusCode.OK ? ex.StatusCode : HttpStatusCode.InternalServerError, stopwatch, (Exception) ex);
            }
      catch (Exception ex)
            {
                return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, HttpStatusCode.InternalServerError, stopwatch, ex);
            }
            return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, HttpStatusCode.Accepted, stopwatch);
        }

        public static HttpResponseMessage LogAndCreateResponse(
            this ICommunicationsClient client,
            HttpRequestMessage request,
            Guid requestId,
            Guid scenarioId,
            CommsNotifications notifications,
            HttpStatusCode statusCode,
            Stopwatch stopwatch,
            Exception exception = null) {
            TraceLevel level = client.GraphLogger.LogHttpRequest(request, statusCode, (object) notifications, exception, nameof(LogAndCreateResponse), "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientExtensions.cs", 151);
            HttpResponseMessage response = client.CreateResponse(statusCode, requestId, scenarioId, (object) exception);
            client.GraphLogger.LogHttpResponse(level, request, response, stopwatch.ElapsedMilliseconds, (object) exception, nameof(LogAndCreateResponse), "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientExtensions.cs", 153);
            stopwatch.Stop();
            return response;
        }

        public static HttpResponseMessage CreateResponse(
            this ICommunicationsClient client,
            HttpStatusCode statusCode,
            Guid requestId,
            Guid scenarioId,
            object responseContent = null) {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(statusCode);
            if (requestId != Guid.Empty)
                httpResponseMessage.Headers.Add("Client-Request-Id", requestId.ToString());
            if (scenarioId != Guid.Empty)
                httpResponseMessage.Headers.Add("Scenario-Id", scenarioId.ToString());
            if (httpResponseMessage.StatusCode != HttpStatusCode.NoContent && responseContent != null) {
                string content = client.Serializer.SerializeObject(responseContent);
                httpResponseMessage.Content = (HttpContent) new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            }
            return httpResponseMessage;
        }
    }
}
