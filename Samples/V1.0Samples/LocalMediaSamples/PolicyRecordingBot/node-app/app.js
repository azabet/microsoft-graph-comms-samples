const WebSocket = require('ws');
const Hapi = require('@hapi/hapi');

const useSSL = false;

const tls = useSSL && {
    cert: fs.readFileSync('./cert.pem'),
    key: fs.readFileSync('./privkey.pem'),
};

function createWebsocketServer() {
    let wss;
    if (useSSL) {
        const server = https.createServer(tls);
        wss = new WebSocket.Server({ server });
        server.listen(8443);
    } else {
        wss = new WebSocket.Server({ port: 8080 });
    }
    wss.on('connection', function connection(ws) {
        ws.on('message', function incoming(message) {
            console.log('received: %s', message);
        });
        ws.send('welcome');
    });
    return wss;
}

const wss = createWebsocketServer();

async function createHapiServer() {
    let server;
    if (useSSL) {
        server = Hapi.server({
            port: 443,
            host: '0.0.0.0',
            tls,
        });
    } else {
        server = Hapi.server({
            port: 80,
            host: '0.0.0.0',
        });
    }
    await server.register(require('@hapi/inert'));
    server.route({
        method: 'GET',
        path: '/',
        handler: (request, h) => h.file('index.html')
    });
    server.route({
        method: 'POST',
        path: '/publish',
        handler: (request, h) => {
            const { payload } = request;
            console.log({ payload });
            wss.clients.forEach(client => {
                if (client.readyState === WebSocket.OPEN) {
                    client.send(JSON.stringify(payload));
                }
            });
            return 'OK'
        }
    });
    await server.start();
    console.log('Server running on %s', server.info.uri);
}

createHapiServer();
