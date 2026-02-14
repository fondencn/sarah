const { env } = require('process');

const httpsFromUrls = env.ASPNETCORE_URLS
  ? env.ASPNETCORE_URLS.split(';').find(url => url.startsWith('https://'))
  : undefined;

const target = env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
  : httpsFromUrls ?? 'https://localhost:7165';

if (env.ASPNETCORE_URLS && !httpsFromUrls && !env.ASPNETCORE_HTTPS_PORT) {
  throw new Error('ASPNETCORE_URLS does not include an https endpoint. HTTPS is required for OIDC.');
}

const PROXY_CONFIG = [
  {
    context: [
      "/weatherforecast",
    ],
    target,
    secure: false
  }
]

module.exports = PROXY_CONFIG;
