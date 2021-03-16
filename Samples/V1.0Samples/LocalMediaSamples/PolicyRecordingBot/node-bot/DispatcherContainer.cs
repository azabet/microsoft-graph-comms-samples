// Microsoft.Graph.Communications.Client.Notifications.DispatcherContainer
// Version=1.2.0.850

using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Core.Notifications;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Graph.Communications.Client.Notifications {
    internal class DispatcherContainer :
    ObjectRootDisposable,
        INotificationDispatcherContainer,
        INotificationDispatcher,
        IDisposable
    {
    private readonly Dictionary < string, INotificationDispatcher > dispatchers = new Dictionary<string, INotificationDispatcher>();
    private readonly object dispatcherContainerLock = new object();

    public DispatcherContainer(IGraphLogger logger)
      : base(logger)
        {
        }

    public event Action < NotificationEventArgs > OnNotificationIncoming;

    public event Action < NotificationEventArgs > OnNotificationProcessed;

    public event Action < NotificationEventArgs > OnNotificationQueued;

    public event Action < FailedNotificationEventArgs > OnNotificationException;

    public void ProcessNotification(NotificationEventArgs args)
        {
            foreach(KeyValuePair < string, INotificationDispatcher > dispatcher in this.dispatchers)
            {
                if (args.Notification.ResourceUrl.TrimStart('/').StartsWith(dispatcher.Key, StringComparison.OrdinalIgnoreCase))
                    dispatcher.Value.ProcessNotification(args);
            }
        }

    public void CompleteNotification(NotificationEventArgs args) => this.ProcessedNotification(args);

    public Task ProcessNotificationAndWaitAsync(NotificationEventArgs args, TimeSpan timeout = default(TimeSpan))
        {
            List < Task > taskList = new List<Task>();
            foreach(KeyValuePair < string, INotificationDispatcher > dispatcher in this.dispatchers)
            {
                if (args.Notification.ResourceUrl.TrimStart('/').StartsWith(dispatcher.Key))
                    taskList.Add(dispatcher.Value.ProcessNotificationAndWaitAsync(args, timeout));
            }
            return taskList.Count <= 0 ? Task.CompletedTask : Task.WhenAll((IEnumerable<Task>) taskList);
        }

    public IDisposable Subscribe(
            string resource,
            INotificationCallback callback,
            bool ignoreBacklog)
        {
            foreach(KeyValuePair < string, INotificationDispatcher > dispatcher in this.dispatchers)
            {
                if (resource.TrimStart('/').StartsWith(dispatcher.Key))
                    return dispatcher.Value.Subscribe(resource, callback, ignoreBacklog);
            }
            return (IDisposable) null;
        }

    public INotificationDispatcher RegisterDispatcherGetOrAdd(
            string baseUrl,
            Func < INotificationDispatcher > dispatcher)
        {
            baseUrl = baseUrl.TrimStart('/');
            INotificationDispatcher notificationDispatcher1;
            if (this.dispatchers.TryGetValue(baseUrl, out notificationDispatcher1))
                return notificationDispatcher1;
            lock(this.dispatcherContainerLock)
            {
                if (this.dispatchers.TryGetValue(baseUrl, out notificationDispatcher1))
                    return notificationDispatcher1;
                this.GraphLogger.Info("Dispatcher Container registering notification dispatcher for: " + baseUrl, memberName: nameof(RegisterDispatcherGetOrAdd), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\Notifications\\DispatcherContainer.cs", lineNumber: 117);
                INotificationDispatcher notificationDispatcher2 = dispatcher();
                this.dispatchers[baseUrl] = notificationDispatcher2;
                notificationDispatcher2.OnNotificationIncoming += new Action<NotificationEventArgs>(this.ReceivedNotification);
                notificationDispatcher2.OnNotificationProcessed += new Action<NotificationEventArgs>(this.ProcessedNotification);
                notificationDispatcher2.OnNotificationQueued += new Action<NotificationEventArgs>(this.QueuedNotification);
                notificationDispatcher2.OnNotificationException += new Action<FailedNotificationEventArgs>(this.FailedNotification);
                return notificationDispatcher2;
            }
        }

    protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            foreach(KeyValuePair < string, INotificationDispatcher > dispatcher in this.dispatchers)
            {
                dispatcher.Value.OnNotificationIncoming -= new Action<NotificationEventArgs>(this.ReceivedNotification);
                dispatcher.Value.OnNotificationProcessed -= new Action<NotificationEventArgs>(this.ProcessedNotification);
                dispatcher.Value.OnNotificationQueued += new Action<NotificationEventArgs>(this.QueuedNotification);
                dispatcher.Value.OnNotificationException -= new Action<FailedNotificationEventArgs>(this.FailedNotification);
                dispatcher.Value.Dispose();
            }
        }

    private void ProcessedNotification(NotificationEventArgs args)
        {
            Action < NotificationEventArgs > notificationProcessed = this.OnNotificationProcessed;
            if (notificationProcessed == null)
                return;
            notificationProcessed(args);
        }

    private void QueuedNotification(NotificationEventArgs args)
        {
            Action < NotificationEventArgs > notificationQueued = this.OnNotificationQueued;
            if (notificationQueued == null)
                return;
            notificationQueued(args);
        }

    private void FailedNotification(FailedNotificationEventArgs args)
        {
            Action < FailedNotificationEventArgs > notificationException = this.OnNotificationException;
            if (notificationException == null)
                return;
            notificationException(args);
        }

    private void ReceivedNotification(NotificationEventArgs args)
        {
            Action < NotificationEventArgs > notificationIncoming = this.OnNotificationIncoming;
            if (notificationIncoming == null)
                return;
            notificationIncoming(args);
        }
    }
}
