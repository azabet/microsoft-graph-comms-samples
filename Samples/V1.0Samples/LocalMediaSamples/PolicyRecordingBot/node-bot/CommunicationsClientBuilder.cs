// Microsoft.Graph.Communications.Client.CommunicationsClientBuilder
// Version=1.2.0.850

using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Client.Cache;
using Microsoft.Graph.Communications.Client.Transport;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common.Telemetry.Obfuscation;
using Microsoft.Graph.Communications.Common.Transport;
using Microsoft.Graph.Communications.Core.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Microsoft.Graph.Communications.Client {
    public class CommunicationsClientBuilder : ICommunicationsClientBuilder
    {
    private static readonly ObfuscationConfiguration ObfuscationConfiguration = (ObfuscationConfiguration) new HashingObfuscationConfiguration(addOdataType: false, members: LogProperties.ObfuscationMembers);
    private static readonly Regex UserAgentNameRegex = new Regex("[^\\w\\d\\s\\.@]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly string appName;
    private readonly string appId;
    private readonly IGraphLogger logger;
    private Uri notificationUri;
    private Uri serviceBaseUrl;
    private HttpClient httpClient;
    private IEnumerable < KeyValuePair < string, string >> defaultHeaders;
    private IOperationManager operationManager;
    private ICache cacheStrategy;
    private IRequestAuthenticationProvider authenticationProvider;

    public CommunicationsClientBuilder(
        string appName,
        string appId,
        IGraphLogger logger = null,
        ObfuscationMember[] additionalObfuscationMembers = null)
      : this(appName, appId, logger, CommunicationsClientBuilder.CreateObfuscationConfiguration(additionalObfuscationMembers))
        {
        }

        internal CommunicationsClientBuilder(
            string appName,
            string appId,
            IGraphLogger logger,
            ObfuscationConfiguration obfuscationConfiguration)
        {
            this.appName = appName.NotNull<string>(nameof(appName));
            this.appId = !string.IsNullOrWhiteSpace(appId) ? appId : throw new ArgumentNullException(nameof(appId));
            AadApplicationIdentity[] applicationIdentityArray = new AadApplicationIdentity[1]
            {
                new AadApplicationIdentity()
                {
                    AppName = this.appName,
                        AppId = this.appId
                }
            };
            this.logger = logger == null ? (IGraphLogger) new GraphLogger("CommunicationsClient", (IEnumerable<object>) applicationIdentityArray, obfuscationConfiguration: (obfuscationConfiguration ?? CommunicationsClientBuilder.ObfuscationConfiguration)) : logger.CreateShim("CommunicationsClient", properties: ((IEnumerable<object>) applicationIdentityArray), obfuscationConfiguration: (obfuscationConfiguration ?? CommunicationsClientBuilder.ObfuscationConfiguration));
        }

    public Guid Id { get; } = Guid.NewGuid();

    public ICommunicationsClient Build()
        {
            if (this.notificationUri == (Uri) null)
            throw new ArgumentException("Cannot build the client without setting the notification URL.");
            if (this.serviceBaseUrl == (Uri) null)
            throw new ArgumentException("Cannot build the client without setting the service base URL.");
            if (this.authenticationProvider == null)
                throw new ArgumentException("Cannot build the client without an authentication provider or client credentials.");
            AssemblyName name1 = this.GetType().Assembly.GetName();
            string str = ((IEnumerable<string>) name1.Name.Split('.')).Skip<string>(1).Join(string.Empty);
            this.logger.Info("Assembly: " + str, memberName: nameof(Build), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientBuilder.cs", lineNumber: ((int) sbyte.MaxValue));
            string pascalCase = CommunicationsClientBuilder.UserAgentNameRegex.Replace(this.appName, "-").ToPascalCase();
            this.logger.Info("Application: " + pascalCase, memberName: nameof(Build), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientBuilder.cs", lineNumber: 130);
            string name2 = str + "-" + pascalCase;
            this.logger.Info("Product: " + name2, memberName: nameof(Build), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientBuilder.cs", lineNumber: 133);
            string version = name1.Version.ToString();
            this.logger.Info("Version: " + version, memberName: nameof(Build), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientBuilder.cs", lineNumber: 136);
            ProductInfoHeaderValue productInfoHeaderValue = new ProductInfoHeaderValue(new ProductHeaderValue(name2, version));
            this.logger.Info(string.Format("UserAgent: {0}", (object) productInfoHeaderValue), memberName: nameof(Build), filePath: "D:\\a\\_work\\40\\s\\SDK\\Microsoft.Graph.Communications.Client\\Client\\CommunicationsClientBuilder.cs", lineNumber: 140);
            CommsSerializer commsSerializer = new CommsSerializer();
            IGraphLogger logger = this.logger;
            JsonSerializerSettings serializerSettings = commsSerializer.JsonSerializerSettings;
            IRequestAuthenticationProvider authenticationProvider = this.authenticationProvider;
            ProductInfoHeaderValue userAgent = productInfoHeaderValue;
            IEnumerable < KeyValuePair < string, string >> defaultHeaders = this.defaultHeaders;
            IEnumerable < IGraphProperty < string >> graphProperties = defaultHeaders != null ? defaultHeaders.Select<KeyValuePair<string, string>, IGraphProperty<string>>((Func<KeyValuePair<string, string>, IGraphProperty<string>>)(pair => GraphProperty.RequestProperty<string>(pair.Key, pair.Value))) : (IEnumerable<IGraphProperty<string>>) null;
            HttpClient httpClient = this.httpClient;
            return (ICommunicationsClient) new CommunicationsClient(this.serviceBaseUrl.AbsoluteUri, new GraphAuthClientFactory(logger, serializerSettings, authenticationProvider, userAgent, (IEnumerable<IGraphProperty>) graphProperties, httpClient), this.operationManager, (ISerializer) commsSerializer, this.cacheStrategy, this.notificationUri)
            {
                Id = this.Id,
                    AppName = this.appName,
                    AppId = this.appId
            };
        }

    public ICommunicationsClientBuilder SetAuthenticationProvider(
            IRequestAuthenticationProvider authenticationProvider)
        {
            this.authenticationProvider = authenticationProvider.NotNull<IRequestAuthenticationProvider>(nameof(authenticationProvider));
            return (ICommunicationsClientBuilder) this;
        }

    public ICommunicationsClientBuilder SetNotificationUrl(
            Uri notificationUrlInput)
        {
            if (notificationUrlInput == (Uri) null)
            throw new ArgumentNullException(nameof(notificationUrlInput));
            this.notificationUri = string.Equals(notificationUrlInput.Scheme, "https", StringComparison.InvariantCultureIgnoreCase) ? notificationUrlInput : throw new ArgumentException("URI must be https.");
            return (ICommunicationsClientBuilder) this;
        }

    public ICommunicationsClientBuilder SetServiceBaseUrl(
            Uri serviceBaseUrlInput)
        {
            if (serviceBaseUrlInput == (Uri) null)
            throw new ArgumentNullException(nameof(serviceBaseUrlInput));
            this.serviceBaseUrl = string.Equals(serviceBaseUrlInput.Scheme, "https", StringComparison.InvariantCultureIgnoreCase) ? serviceBaseUrlInput : throw new ArgumentException("URI must be https.");
            return (ICommunicationsClientBuilder) this;
        }

    public ICommunicationsClientBuilder SetHttpClient(
            HttpClient httpClient,
            IEnumerable < KeyValuePair < string, string >> defaultHeaders = null)
        {
            this.httpClient = httpClient.NotNull<HttpClient>(nameof(httpClient));
            this.defaultHeaders = defaultHeaders;
            return (ICommunicationsClientBuilder) this;
        }

    public ICommunicationsClientBuilder SetCacheStrategy(
            ICache cacheStrategy)
        {
            this.cacheStrategy = cacheStrategy.NotNull<ICache>(nameof(cacheStrategy));
            return (ICommunicationsClientBuilder) this;
        }

        internal CommunicationsClientBuilder SetOperationManager(
            IOperationManager manager)
        {
            this.operationManager = manager.NotNull<IOperationManager>(nameof(manager));
            return this;
        }

    private static ObfuscationConfiguration CreateObfuscationConfiguration(
            params ObfuscationMember[] additionalMembers)
        {
            return (additionalMembers != null ? (!((IEnumerable<ObfuscationMember>) additionalMembers).Any<ObfuscationMember>() ? 1 : 0) : 1) != 0 ? (ObfuscationConfiguration) null : (ObfuscationConfiguration) new HashingObfuscationConfiguration(addOdataType: false, members: CommunicationsClientBuilder.MergeMembers((IEnumerable<ObfuscationMember>) LogProperties.ObfuscationMembers, (IEnumerable<ObfuscationMember>) additionalMembers).ToArray<ObfuscationMember>());
        }

    private static IEnumerable < ObfuscationMember > MergeMembers(
            IEnumerable < ObfuscationMember > members1,
            IEnumerable < ObfuscationMember > members2)
        {
            if (members1 == null)
                return members2;
            if (members2 == null)
                return members1;
            IDictionary < string, ObfuscationMember > obfuscationMembers = (IDictionary<string, ObfuscationMember>) members1.ToDictionary<ObfuscationMember, string>((Func<ObfuscationMember, string>)(member => member.Name));
            members2.ForEach<ObfuscationMember>((Action<ObfuscationMember>)(newMember => {
                ObfuscationMember obfuscationMember;
                if (obfuscationMembers.TryGetValue(newMember.Name, out obfuscationMember)) {
                    ref ObfuscationMember local = ref newMember;
                    IEnumerable < ObfuscationMember > source = CommunicationsClientBuilder.MergeMembers((IEnumerable<ObfuscationMember>) newMember.Members, (IEnumerable<ObfuscationMember>) obfuscationMember.Members);
                    ObfuscationMember[] obfuscationMemberArray = source != null ? source.ToArray<ObfuscationMember>() : (ObfuscationMember[]) null;
                    local.Members = obfuscationMemberArray;
                }
                else
                    obfuscationMembers.Add(newMember.Name, newMember);
            }));
            return (IEnumerable<ObfuscationMember>) obfuscationMembers.Values;
        }
    }
}
