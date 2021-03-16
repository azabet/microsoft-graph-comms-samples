'use strict';

const Hapi = require('@hapi/hapi');
const fs = require('fs');
const axios = require('axios');
const graphClient = require('./graphClient');
console.log({ graphClient });

const tls = {
    cert: fs.readFileSync('./cert.pem'),
    key: fs.readFileSync('./privkey.pem'),
};


const logs = [];

const getResourceUrl = payload => payload.value[0].resourceUrl;

async function answerCall(resourceUrl) {
    const callbackUri = 'https://teams.featherinthecap.com/notification';
    const answer = {
        callbackUri,
        mediaConfig: {
            '@odata.type': "#microsoft.graph.appHostedMediaConfig",
            blob: "<Media Session Configuration Blob>"
        },
        acceptedModalities: [
            "audio"
        ]
    };
    const r = await graphClient.api(resourceUrl + '/answer').post(answer);
    console.log({ r });
}

async function createServer() {
    const server = Hapi.server({ port: 443, host: '0.0.0.0', tls });
    server.route({
        method: 'GET',
        path: '/',
        handler: (request, h) => 'Hello'
    });
    server.route({
        method: 'GET',
        path: '/logs',
        handler: (request, h) => logs
    });
    server.route({
        method: 'GET',
        path: '/test',
        handler: (request, h) => {
            const response = h.response().code(202);
            response.headers = {
                'Client-Request-Id': '123',
                'Scenario-Id': 'abc',
                'connection': 'close',
            };
            return response;
        }
    });
    server.route({
        method: 'POST',
        path: '/api/calling',
        handler: (request, h) => {
            const { path, headers, payload } = request;
            logs.push({ path, headers, payload });
            const requestId = headers['x-microsoft-skype-message-id']; // or 'Client-Request-Id'
            const scenarioId = headers['x-microsoft-skype-chain-id']; // or 'Scenario-Id'
            const response = h.response().code(202);
            response.headers = {
                'Client-Request-Id': requestId,
                'Scenario-Id': scenarioId,
                'connection': 'close',
            };
            const resourceUrl = getResourceUrl(payload);
            answerCall(resourceUrl);
            return response;
        }
    });
    server.route({
        method: 'POST',
        path: '/api/calling/notification',
        handler: (request, h) => {
            const { path, headers, payload } = request;
            logs.push({ path, headers, payload });
            return 'OK';
        }
    });
    server.route({
        method: 'POST',
        path: '/notification',
        handler: (request, h) => {
            const { path, headers, payload } = request;
            logs.push({ path, headers, payload });
            return 'OK';
        }
    });
    
    await server.start();
    console.log('Server running on %s', server.info.uri);
}

createServer();
