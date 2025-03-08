require('./instrumentation.js'); // Ensure this is imported before other modules
//require('@opentelemetry/auto-instrumentations-node/register');

const http = require('node:http');
const https = require('node:https');
const express = require('express');
const fs = require("node:fs");
const { createTerminus, HealthCheckError } = require('@godaddy/terminus');

const config = {
    environment: process.env.NODE_ENV || 'development',
    httpPort: process.env['PORT'] ?? 8080,
    httpsPort: process.env['HTTPS_PORT'] ?? 8443,
    httpsRedirectPort: process.env['HTTPS_REDIRECT_PORT'] ?? (process.env['HTTPS_PORT'] ?? 8443),
    certFile: process.env['HTTPS_CERT_FILE'] ?? '',
    certKeyFile: process.env['HTTPS_CERT_KEY_FILE'] ?? '',
};
console.log(`config: ${JSON.stringify(config)}`);

// Setup HTTPS options
const httpsOptions = fs.existsSync(config.certFile) && fs.existsSync(config.certKeyFile)
    ? {
        cert: fs.readFileSync(config.certFile),
        key: fs.readFileSync(config.certKeyFile),
        enabled: true
    }
    : { enabled: false };

const app = express();

function httpsRedirect(req, res, next) {
    if (req.secure || req.headers['x-forwarded-proto'] === 'https') {
        // Request is already HTTPS
        return next();
    }
    // Redirect to HTTPS
    const redirectTo = new URL(`https://${process.env.HOST ?? 'localhost'}:${config.httpsRedirectPort}${req.url}`);
    console.log(`Redirecting to ${redirectTo}`);
    res.redirect(redirectTo);
}
if (httpsOptions.enabled) {
    app.use(httpsRedirect);
}

// Define health check callback
async function healthCheck() {
    const errors = [];
    const apiServerHealthAddress = `${config.apiServer}/health`;
    console.log(`Fetching ${apiServerHealthAddress}`);
    try {
        var response = await fetch(apiServerHealthAddress);
        if (!response.ok) {
            console.log(`Failed fetching ${apiServerHealthAddress}. ${response.status}`);
            throw new HealthCheckError(`Fetching ${apiServerHealthAddress} failed with HTTP status: ${response.status}`);
        }
    } catch (error) {
        console.log(`Failed fetching ${apiServerHealthAddress}. ${error}`);
        throw new HealthCheckError(`Fetching ${apiServerHealthAddress} failed with HTTP status: ${error}`);
    }
}
app.get('/', function (req, res) {
    res.send('Hello World!');
});
// Start a server
function startServer(server, port) {
    if (server) {
        const serverType = server instanceof https.Server ? 'HTTPS' : 'HTTP';

        // Create the health check endpoint
        createTerminus(server, {
            signal: 'SIGINT',
            healthChecks: {
                '/health': healthCheck,
                '/alive': () => { }
            },
            onSignal: async () => {
                console.log('server is starting cleanup');
            },
            onShutdown: () => console.log('cleanup finished, server is shutting down')
        });

        // Start the server
        server.listen(port, () => {
            console.log(`${serverType} listening on ${JSON.stringify(server.address())}`);
        });
    }
}

const httpServer = http.createServer(app);
const httpsServer = httpsOptions.enabled ? https.createServer(httpsOptions, app) : null;

startServer(httpServer, config.httpPort);
startServer(httpsServer, config.httpsPort);

//app.listen(port, function () {
//    console.log(`Example app listening on port ${port}!`);
//});