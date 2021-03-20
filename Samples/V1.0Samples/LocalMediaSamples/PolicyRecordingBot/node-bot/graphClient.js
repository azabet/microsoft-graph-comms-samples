const msal = require('@azure/msal-node');
require('isomorphic-fetch');
const { Client, ImplicitMSALAuthenticationProvider, MSALAuthenticationProviderOptions } = require("@microsoft/microsoft-graph-client");

const OAUTH_APP_ID = '';
const OAUTH_APP_SECRET = '';
const OAUTH_SCOPES = 'user.read,calendars.readwrite,mailboxsettings.read,Calls.AccessMedia.All,Calls.JoinGroupCall.All';

const msalConfig = {
    auth: {
        clientId: OAUTH_APP_ID,
        clientSecret: OAUTH_APP_SECRET,
    },
    system: {
        loggerOptions: {
            loggerCallback(loglevel, message, containsPii) {
                console.log(message);
            },
            piiLoggingEnabled: false,
            logLevel: msal.LogLevel.Verbose,
        }
    }
};

const msalApp = new msal.ConfidentialClientApplication(msalConfig);

const graphScopes = OAUTH_SCOPES.split(',');

const options = new MSALAuthenticationProviderOptions(graphScopes);
const authProvider = new ImplicitMSALAuthenticationProvider(msalApp, options);

const client = Client.initWithMiddleware({ authProvider });

module.exports = client;
