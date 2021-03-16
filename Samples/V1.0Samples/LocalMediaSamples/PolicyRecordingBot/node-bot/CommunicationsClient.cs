// Microsoft.Graph.Communications.Client.CommunicationsClient
// Version=1.2.0.850

using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Client.Cache;
using Microsoft.Graph.Communications.Client.Notifications;
using Microsoft.Graph.Communications.Client.Transport;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common.Transport;
using Microsoft.Graph.Communications.Core.Notifications;
using Microsoft.Graph.Communications.Core.Serialization;
using Microsoft.Graph.Communications.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Graph.Communications.Client {
    internal sealed class CommunicationsClient :
    ObjectRootDisposable,
        IInternalCommunicationsClient,
        ICommunicationsClient,
        IDisposable
    {
    private readonly IDictionary < string, IResourceCollection > resourceCollections = (IDictionary<string, IResourceCollection>) new Dictionary<string, IResourceCollection>();

    public CommunicationsClient(
        string baseUrl,
        GraphAuthClientFactory graphClientFactory,
        IOperationManager operationManager = null,
        ISerializer serializer = null,
        ICache cacheStrategy = null,
        Uri notificationUri = null)
      : base(graphClientFactory?.GraphLogger)
        {
            this.BaseUrl = baseUrl.NotNullOrWhitespace(nameof(baseUrl));
            this.GraphClientFactory = (IGraphClientFactory) graphClientFactory.NotNull<GraphAuthClientFactory>(nameof(graphClientFactory));
            this.GraphClient = graphClientFactory.Create((IGraphLogger) null);
            this.AuthenticationProvider = graphClientFactory.AuthenticationProvider;
            this.NotificationDispatcher = (INotificationDispatcherContainer) new DispatcherContainer(this.GraphLogger);
            this.OperationManager = operationManager ?? (IOperationManager) new Microsoft.Graph.Communications.Client.OperationManager(graphClientFactory.GraphLogger);
            this.Serializer = serializer ?? (ISerializer) new CommsSerializer();
            this.DefaultCache = cacheStrategy ?? (ICache) new ServiceCache(this.GraphLogger, this.GraphClient, this.Serializer, baseUrl);
            this.NotificationUrl = notificationUri;
            if (!(notificationUri == (Uri) null))
            return;
            this.GraphLogger.Warn("Notification uri is null. Make sure you set Notification URIs on resources", memberName: ".ctor", filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClient.cs", lineNumber: 71);
        }

    public event Action < NotificationEventArgs > OnNotificationProcessed
        {
            add => this.NotificationDispatcher.OnNotificationProcessed += value;
            remove => this.NotificationDispatcher.OnNotificationProcessed -= value;
        }

    public event Action < NotificationEventArgs > OnNotificationQueued
        {
            add => this.NotificationDispatcher.OnNotificationQueued += value;
            remove => this.NotificationDispatcher.OnNotificationQueued -= value;
        }

    public event Action < FailedNotificationEventArgs > OnNotificationException
        {
            add => this.NotificationDispatcher.OnNotificationException += value;
            remove => this.NotificationDispatcher.OnNotificationException -= value;
        }

    public Guid Id { get; set; }

    public string AppName { get; set; }

    public string AppId { get; set; }

    public string BaseUrl { get; }

    public new IGraphLogger GraphLogger => base.GraphLogger;

    public IGraphClient GraphClient { get; }

    public IRequestAuthenticationProvider AuthenticationProvider { get; }

    public IGraphClientFactory GraphClientFactory { get; }

    public INotificationDispatcherContainer NotificationDispatcher { get; }

    public IOperationManager OperationManager { get; }

    public ISerializer Serializer { get; }

    public ICache DefaultCache { get; }

    public Uri NotificationUrl { get; }

    public void ProcessNotifications(
            Uri callbackUri,
            CommsNotifications notifications,
            string tenantId,
            Guid requestId,
            Guid scenarioId,
            IDictionary < string, object > additionalData = null)
        {
            foreach(CommsNotification notification in notifications.Value)
            {
                object resouceData = notification.ExtractResouceData();
                NotificationEventArgs args = resouceData is IReadOnlyList < object > resourceData ? (NotificationEventArgs) new CollectionNotificationEventArgs(callbackUri, notification, notification.ChangeType.Value, resourceData) : new NotificationEventArgs(callbackUri, notification, notification.ChangeType.Value, resouceData);
                args.TenantId = tenantId;
                args.RequestId = requestId;
                args.ScenarioId = scenarioId;
                args.AdditionalData = additionalData ?? (IDictionary<string, object>) new Dictionary<string, object>((IEqualityComparer<string>) StringComparer.OrdinalIgnoreCase);
                args.AdditionalData[nameof(callbackUri)] = (object) callbackUri;
                if (this.OperationManager.ProcessNotification(args))
                    this.NotificationDispatcher.CompleteNotification(args);
                else
                    this.NotificationDispatcher.ProcessNotification(args);
            }
        }

    public async Task RehydrateAsync(string resourcePath, string tenantId, Guid scenarioId = default(Guid))
        {
            resourcePath = resourcePath.NotNullOrWhitespace(nameof(resourcePath)).SanitizeResource(this.BaseUrl);
            IResourceCollection[] array = this.resourceCollections.Values.ToArray<IResourceCollection>();
            List < Task < IEnumerable < IInternalResourceBase >>> tasks = new List<Task<IEnumerable<IInternalResourceBase>>>();
            Func < IResourceCollection, bool > predicate = (Func<IResourceCollection, bool>)(r => resourcePath.StartsWith(r.ResourcePath, StringComparison.OrdinalIgnoreCase));
            ((IEnumerable<IResourceCollection>) array).Where<IResourceCollection>(predicate).Select<IResourceCollection, IInternalResourceBase>((Func<IResourceCollection, IInternalResourceBase>)(r => r as IInternalResourceBase)).Where<IInternalResourceBase>((Func<IInternalResourceBase, bool>)(r => r != null)).ForEach<IInternalResourceBase>((Action<IInternalResourceBase>)(r => tasks.Add(r.RehydrateAsync(resourcePath, tenantId, scenarioId))));
            if (tasks.Count <= 0)
                return;
            ((IEnumerable<IEnumerable<IInternalResourceBase>>) await Task.WhenAll<IEnumerable<IInternalResourceBase>>((IEnumerable<Task<IEnumerable<IInternalResourceBase>>>) tasks).ConfigureAwait(false)).Where<IEnumerable<IInternalResourceBase>>((Func<IEnumerable<IInternalResourceBase>, bool>)(c => c != null && c.Any<IInternalResourceBase>())).SelectMany<IEnumerable<IInternalResourceBase>, IInternalResourceBase>((Func<IEnumerable<IInternalResourceBase>, IEnumerable<IInternalResourceBase>>)(c => c)).Where<IInternalResourceBase>((Func<IInternalResourceBase, bool>)(r => r != null)).ForEach<IInternalResourceBase>((Action<IInternalResourceBase>)(r => r.InitializeNotificationSubscription(true)));
        }

    public T GetOrAddResourceCollection<T>(bool maintainState, Func < T > valueFactory) where T: IResourceCollection
        {
            string key = string.Format("{0}, MaintainState={1}", (object) typeof (T).AssemblyQualifiedName, (object) maintainState);
            IResourceCollection resourceCollection1;
            if (this.resourceCollections.TryGetValue(key, out resourceCollection1))
                return (T) resourceCollection1;
            lock(this.resourceCollections)
            {
                if (this.resourceCollections.TryGetValue(key, out resourceCollection1))
                    return (T) resourceCollection1;
                IResourceCollection resourceCollection2 = (IResourceCollection) valueFactory();
                this.resourceCollections.Add(key, resourceCollection2);
                return (T) resourceCollection2;
            }
        }

    public async Task < bool > TerminateAsync(TimeSpan timeout = default(TimeSpan))
        {
            bool ret = true;
            foreach(KeyValuePair < string, IResourceCollection > resourceCollection in (IEnumerable<KeyValuePair<string, IResourceCollection>>) this.resourceCollections)
            {
                if (!await resourceCollection.Value.TerminateAsync(timeout).ConfigureAwait(false))
                    ret = false;
            }
            return ret;
        }

    protected override void Dispose(bool disposing)
        {
            if (disposing) {
                this.NotificationDispatcher?.Dispose();
                this.OperationManager?.Dispose();
                foreach(KeyValuePair < string, IResourceCollection > resourceCollection in (IEnumerable<KeyValuePair<string, IResourceCollection>>) this.resourceCollections)
                resourceCollection.Value.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
