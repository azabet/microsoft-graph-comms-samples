// Microsoft.Graph.Communications.Calls.StatefulCall
// Version=1.2.0.850

using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Client.Transport;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common.Transport;
using Microsoft.Graph.Communications.Core.Notifications;
using Microsoft.Graph.Communications.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Graph.Communications.Calls
{
    internal sealed class StatefulCall :
      StatefulResource<ICall, Call, ICallRequestBuilder>,
      ICall,
      IResource<ICall, Call>,
      IResource,
      IResourceBase,
      IDisposable
    {
        public StatefulCall(
          IInternalCommunicationsClient client,
          IGraphClient graphClient,
          Call resource,
          Guid scenarioId,
          ICallRequestBuilder uriBuilder,
          bool maintainState)
          : base(client, graphClient, resource, uriBuilder)
        {
            this.ScenarioId = scenarioId;
            this.Participants = (IParticipantCollection)new StatefulParticipantCollection(client, graphClient, uriBuilder.Participants, maintainState);
        }

        public IParticipantCollection Participants { get; }

        public string TenantId => this.Resource.TenantId;

        public Guid CorrelationId => this.ScenarioId;

        public Guid ScenarioId { get; private set; }

        public IMediaSession MediaSession { get; set; }

        public override void InitializeNotificationSubscription(bool ignoreBacklog)
        {
            base.InitializeNotificationSubscription(ignoreBacklog);
            this.Participants.InitializeNotificationSubscription(ignoreBacklog);
        }

        public async Task AnswerAsync(
          MediaConfig mediaConfig,
          IEnumerable<Modality> acceptedModalities = null,
          string callbackUri = null,
          Guid scenarioId = default(Guid),
          CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall statefulCall = this;
            statefulCall.UpdateScenarioId(scenarioId);
            if (string.IsNullOrEmpty(callbackUri))
            {
                statefulCall.GraphLogger.Log(TraceLevel.Info, "No callback uri specified, using default notification uri", memberName: nameof(AnswerAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: ((int)sbyte.MaxValue));
                callbackUri = statefulCall.InternalClient.NotificationUrl.ToString();
            }
            statefulCall.GraphLogger.Log(TraceLevel.Info, "Answering call with id: " + statefulCall.Resource.Id, memberName: nameof(AnswerAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 131);
            ICallAnswerRequest callAnswerRequest = statefulCall.UriBuilder.Answer(callbackUri, mediaConfig.NotNull<MediaConfig>(nameof(mediaConfig), "Media configuration must be specified when answering call with id: " + statefulCall.Resource.Id), acceptedModalities).Request();
            await statefulCall.GraphClient.SendAsync((IBaseRequest)callAnswerRequest, RequestType.Create, cancellationToken: cancellationToken).ConfigureAwait(false);
            statefulCall.GraphLogger.Log(TraceLevel.Info, "Answering call with id: " + statefulCall.Resource.Id + " complete.", memberName: nameof(AnswerAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 157);
        }

        public async Task RedirectAsync(
          IEnumerable<InvitationParticipantInfo> targets,
          int? timeout = null,
          string callbackUri = null,
          Guid scenarioId = default(Guid),
          CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall statefulCall = this;
            statefulCall.UpdateScenarioId(scenarioId);
            statefulCall.GraphLogger.Log(TraceLevel.Info, "Redirecting call with id: " + statefulCall.Resource.Id, memberName: nameof(RedirectAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 179);
            if (string.IsNullOrEmpty(callbackUri))
            {
                statefulCall.GraphLogger.Log(TraceLevel.Info, "No callback uri specified, using default notification uri", memberName: nameof(RedirectAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 182);
                callbackUri = statefulCall.InternalClient.NotificationUrl.ToString();
            }
            ICallRedirectRequest callRedirectRequest = statefulCall.UriBuilder.Redirect(targets.NotEmpty<IEnumerable<InvitationParticipantInfo>>(nameof(targets), "There must be at least one target specified when redirecting call with id: " + statefulCall.Resource.Id), timeout, callbackUri).Request();
            await statefulCall.GraphClient.SendAsync((IBaseRequest)callRedirectRequest, RequestType.Create, cancellationToken: cancellationToken).ConfigureAwait(false);
            statefulCall.GraphLogger.Log(TraceLevel.Info, "Redirecting call with id: " + statefulCall.Resource.Id + " complete.", memberName: nameof(RedirectAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 225);
        }

        public async Task RejectAsync(
          RejectReason? rejectReason = null,
          string callbackUri = null,
          Guid scenarioId = default(Guid),
          CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall statefulCall = this;
            statefulCall.UpdateScenarioId(scenarioId);
            statefulCall.GraphLogger.Log(TraceLevel.Info, "Rejecting call with id: " + statefulCall.Resource.Id, memberName: nameof(RejectAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 237);
            if (string.IsNullOrEmpty(callbackUri))
            {
                statefulCall.GraphLogger.Log(TraceLevel.Info, "No callback uri specified, using default notification uri", memberName: nameof(RejectAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 240);
                callbackUri = statefulCall.InternalClient.NotificationUrl.ToString();
            }
            ICallRejectRequest callRejectRequest = statefulCall.UriBuilder.Reject(rejectReason, callbackUri).Request();
            await statefulCall.GraphClient.SendAsync((IBaseRequest)callRejectRequest, RequestType.Create, cancellationToken: cancellationToken).ConfigureAwait(false);
            statefulCall.GraphLogger.Log(TraceLevel.Info, "Rejecting call with id: " + statefulCall.Resource.Id + " complete.", memberName: nameof(RejectAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 251);
        }

        public async Task TransferAsync(
          InvitationParticipantInfo target,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall statefulCall = this;
            statefulCall.GraphLogger.Log(TraceLevel.Info, "Transfering call with id: " + statefulCall.Resource.Id, memberName: nameof(TransferAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 262);
            ICallTransferRequest callTransferRequest = statefulCall.UriBuilder.Transfer(target.NotNull<InvitationParticipantInfo>(nameof(target), "Target must be specified when transfering call with id: " + statefulCall.Resource.Id)).Request();
            await statefulCall.GraphClient.SendAsync((IBaseRequest)callTransferRequest, RequestType.Create, cancellationToken: cancellationToken).ConfigureAwait(false);
            statefulCall.GraphLogger.Log(TraceLevel.Info, "Transfering call with id: " + statefulCall.Resource.Id + " complete.", memberName: nameof(TransferAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 284);
        }

        public async Task<PlayOperationResult> PlayPromptAsync(
          IEnumerable<MediaPrompt> prompts,
          Action promptsQueued = null,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall resource = this;
            resource.GraphLogger.Log(TraceLevel.Info, "Playing prompts of call with id: " + resource.Resource.Id, memberName: nameof(PlayPromptAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 296);
            ICallPlayPromptRequest playPromptRequest = resource.UriBuilder.PlayPrompt((IEnumerable<Prompt>)prompts.NotEmpty<IEnumerable<MediaPrompt>>(nameof(prompts), "Prompts must be specified for playing for call " + resource.Resource.Id + ".")).Request();
            PlayPromptOperation playPromptOperation = await resource.SendRequestAndWaitForCompletionAsync<PlayPromptOperation>((IBaseRequest)playPromptRequest, new TimeSpan?(TimeSpan.FromHours(24.0)), (Action<PlayPromptOperation>)(response =>
            {
                Action action = promptsQueued;
                if (action == null)
                    return;
                action();
            }), cancellationToken: cancellationToken).ConfigureAwait(false);
            resource.GraphLogger.Log(TraceLevel.Info, "Playing prompts of call with id: " + resource.Resource.Id + " complete.", memberName: nameof(PlayPromptAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 320);
            return new PlayOperationResult(playPromptOperation.ResultInfo);
        }

        public async Task SubscribeToToneAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall resource = this;
            resource.GraphLogger.Log(TraceLevel.Info, "Subscribing to tone of call with id: " + resource.Resource.Id, memberName: nameof(SubscribeToToneAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 347);
            ICallSubscribeToToneRequest subscribeToToneRequest = resource.UriBuilder.SubscribeToTone().Request();
            CommsOperation commsOperation = await resource.SendRequestAndWaitForCompletionAsync((IBaseRequest)subscribeToToneRequest, new TimeSpan?(TimeSpan.FromSeconds(30.0)), cancellationToken: cancellationToken).ConfigureAwait(false);
            resource.GraphLogger.Log(TraceLevel.Info, "Subscribing to tone of call with id: " + resource.Resource.Id + " complete.", memberName: nameof(SubscribeToToneAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 356);
        }

        public async Task ChangeScreenSharingRoleAsync(
          ScreenSharingRole role,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall statefulCall = this;
            statefulCall.GraphLogger.Log(TraceLevel.Info, string.Format("Changing sharing role in call with id: {0} to {1}", (object)statefulCall.Resource.Id, (object)role), memberName: nameof(ChangeScreenSharingRoleAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 364);
            statefulCall.MediaSession.NotNull<IMediaSession>("Cannot perform change screen sharing role in non-local media scenarios :" + statefulCall.Resource.Id);
            ((IEnumerable<Modality>)statefulCall.MediaSession.Modalities).VerifyContains<Modality>(Modality.VideoBasedScreenSharing);
            ICallChangeScreenSharingRoleRequest sharingRoleRequest = statefulCall.UriBuilder.ChangeScreenSharingRole(role).Request();
            await statefulCall.GraphClient.SendAsync((IBaseRequest)sharingRoleRequest, RequestType.Create, cancellationToken: cancellationToken).ConfigureAwait(false);
            statefulCall.GraphLogger.Log(TraceLevel.Info, string.Format("Changing sharing role in call with id: {0} to {1} completed", (object)statefulCall.Resource.Id, (object)role), memberName: nameof(ChangeScreenSharingRoleAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 374);
        }

        public async Task KeepAliveAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall statefulCall = this;
            statefulCall.GraphLogger.Log(TraceLevel.Info, "Sending keep alive for call with id: " + statefulCall.Resource.Id, memberName: nameof(KeepAliveAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 381);
            GraphRequest graphRequest = new GraphRequest(new Uri(statefulCall.UriBuilder.AppendSegmentToRequestUrl("keepAlive")), RequestType.Create);
            graphRequest.Properties.Add((IGraphProperty)GraphProperty.ContentProperty<string>("Content-Type", "application/json; charset=utf-8"));
            IGraphResponse graphResponse = await statefulCall.GraphClient.SendAsync<NoContentMessage>((IGraphRequest<NoContentMessage>)graphRequest, cancellationToken).ConfigureAwait(false);
            statefulCall.GraphLogger.Log(TraceLevel.Info, "Sending keep alive for call with id: " + statefulCall.Resource.Id + " completed", memberName: nameof(KeepAliveAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 400);
        }

        public async Task MuteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall resource = this;
            resource.GraphLogger.Log(TraceLevel.Info, "Mute self participant for call with id: " + resource.Resource.Id, memberName: nameof(MuteAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 407);
            ICallMuteRequest callMuteRequest = resource.UriBuilder.Mute().Request();
            CommsOperation commsOperation = await resource.SendRequestAndWaitForCompletionAsync((IBaseRequest)callMuteRequest, new TimeSpan?(TimeSpan.FromSeconds(30.0)), cancellationToken: cancellationToken).ConfigureAwait(false);
            resource.GraphLogger.Log(TraceLevel.Info, "Mute self participant for call with id: " + resource.Resource.Id + " complete.", memberName: nameof(MuteAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 416);
        }

        public async Task UnmuteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall resource = this;
            resource.GraphLogger.Log(TraceLevel.Info, "Unmuting self participant for call with id: " + resource.Resource.Id, memberName: nameof(UnmuteAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 423);
            ICallUnmuteRequest callUnmuteRequest = resource.UriBuilder.Unmute().Request();
            CommsOperation commsOperation = await resource.SendRequestAndWaitForCompletionAsync((IBaseRequest)callUnmuteRequest, new TimeSpan?(TimeSpan.FromSeconds(30.0)), cancellationToken: cancellationToken).ConfigureAwait(false);
            resource.GraphLogger.Log(TraceLevel.Info, "Unmuting self participant for call with id: " + resource.Resource.Id + " complete.", memberName: nameof(UnmuteAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 432);
        }

        public async Task UpdateRecordingStatusAsync(
          RecordingStatus status,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall resource = this;
            resource.GraphLogger.Log(TraceLevel.Info, "Updating recording state for call with id: " + resource.Resource.Id, memberName: nameof(UpdateRecordingStatusAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 459);
            ICallUpdateRecordingStatusRequest recordingStatusRequest = resource.UriBuilder.UpdateRecordingStatus(status).Request();
            CommsOperation commsOperation = await resource.SendRequestAndWaitForCompletionAsync((IBaseRequest)recordingStatusRequest, new TimeSpan?(TimeSpan.FromSeconds(30.0)), cancellationToken: cancellationToken).ConfigureAwait(false);
            resource.GraphLogger.Log(TraceLevel.Info, "Updating recording state for call with id: " + resource.Resource.Id + " complete.", memberName: nameof(UpdateRecordingStatusAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 468);
        }

        public async Task<RecordOperationResult> RecordResponseAsync(
          int? maxRecordDurationInSeconds = null,
          int? initialSilenceTimeoutInSeconds = null,
          int? maxSilenceTimeoutInSeconds = null,
          bool? bargeInAllowed = null,
          bool? playBeep = null,
          IEnumerable<Prompt> prompts = null,
          IEnumerable<string> stopTones = null,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            StatefulCall resource = this;
            resource.GraphLogger.Log(TraceLevel.Info, "Recording for call with id: " + resource.Resource.Id, memberName: nameof(RecordResponseAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 557);
            ICallRecordResponseRequest recordResponseRequest = resource.UriBuilder.RecordResponse(prompts, bargeInAllowed, initialSilenceTimeoutInSeconds, maxSilenceTimeoutInSeconds, maxRecordDurationInSeconds, playBeep, stopTones).Request();
            RecordOperation recordOperation = await resource.SendRequestAndWaitForCompletionAsync<RecordOperation>((IBaseRequest)recordResponseRequest, new TimeSpan?(TimeSpan.FromHours(24.0)), cancellationToken: cancellationToken).ConfigureAwait(false);
            resource.GraphLogger.Log(TraceLevel.Info, "Recording for call with id: " + resource.Resource.Id + " complete.", memberName: nameof(RecordResponseAsync), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 576);
            return new RecordOperationResult(recordOperation.RecordingLocation, recordOperation.RecordingAccessToken, recordOperation.ResultInfo);
        }

        public override async Task<IEnumerable<IInternalResourceBase>> RehydrateAsync(
          string resourcePath,
          string tenantId,
          Guid scenarioId,
          CancellationToken cancellationToken)
        {
            List<Task<IEnumerable<IInternalResourceBase>>> taskList = new List<Task<IEnumerable<IInternalResourceBase>>>();
            if (this.Participants is IInternalResourceBase participants)
                taskList.Add(participants.RehydrateAsync(participants.ResourcePath, scenarioId: Guid.Empty, cancellationToken: cancellationToken));
            return ((IEnumerable<IEnumerable<IInternalResourceBase>>)await Task.WhenAll<IEnumerable<IInternalResourceBase>>((IEnumerable<Task<IEnumerable<IInternalResourceBase>>>)taskList).ConfigureAwait(false)).Where<IEnumerable<IInternalResourceBase>>((Func<IEnumerable<IInternalResourceBase>, bool>)(c => c != null && c.Any<IInternalResourceBase>())).SelectMany<IEnumerable<IInternalResourceBase>, IInternalResourceBase>((Func<IEnumerable<IInternalResourceBase>, IEnumerable<IInternalResourceBase>>)(c => c)).Where<IInternalResourceBase>((Func<IInternalResourceBase, bool>)(r => r != null));
        }

        public override void NotificationReceived(NotificationEventArgs args)
        {
            if (args?.ResourceData is Call resourceData)
            {
                resourceData.TenantId = args.TenantId ?? resourceData.TenantId;
                this.ScenarioId = args.ScenarioId == Guid.Empty ? this.ScenarioId : args.ScenarioId;
            }
            base.NotificationReceived(args);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.MediaSession?.Dispose();
                this.MediaSession = (IMediaSession)null;
                this.Participants.Dispose();
            }
            base.Dispose(disposing);
        }

        private void UpdateScenarioId(Guid scenarioId)
        {
            Guid scenarioId1 = this.ScenarioId;
            if (scenarioId == Guid.Empty || scenarioId == scenarioId1)
                return;
            this.GraphLogger.Info(string.Format("Updating ICall scenario identifier from {0} to {1}.", (object)scenarioId1, (object)scenarioId), memberName: nameof(UpdateScenarioId), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 700);
            this.ScenarioId = scenarioId;
            this.GraphLogger.CorrelationId = scenarioId;
            if (this.GraphClient is GraphClientWrapper graphClient)
                graphClient.Context.ScenarioId = scenarioId;
            this.GraphLogger.Info(string.Format("Updated ICall scenario identifier from {0} to {1}.", (object)scenarioId1, (object)scenarioId), memberName: nameof(UpdateScenarioId), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Calls\\Models\\StatefulCall.cs", lineNumber: 710);
        }
    }
}
