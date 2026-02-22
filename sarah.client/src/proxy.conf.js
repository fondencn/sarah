const { env } = require('process');

const httpFromUrls = env.ASPNETCORE_URLS
  ? env.ASPNETCORE_URLS.split(';').find(url => url.startsWith('http://'))
  : undefined;

const target = env.ASPNETCORE_HTTP_PORT
  ? `http://localhost:${env.ASPNETCORE_HTTP_PORT}`
  : httpFromUrls ?? 'http://localhost:7165';

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
