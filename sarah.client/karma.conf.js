const fs = require('fs');

if (!process.env.CHROME_BIN) {
  const chromeCandidates = [
    '/snap/chromium/current/usr/lib/chromium-browser/chrome',
    '/usr/bin/chromium-browser',
    '/usr/bin/chromium',
    '/snap/bin/chromium'
  ];

  const detectedChromeBin = chromeCandidates.find((candidate) => fs.existsSync(candidate));
  if (detectedChromeBin) {
    process.env.CHROME_BIN = detectedChromeBin;
  }
}

module.exports = function (config) {
  config.set({
    basePath: '',
    frameworks: ['jasmine', '@angular-devkit/build-angular'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('@angular-devkit/build-angular/plugins/karma')
    ],
    client: {
      clearContext: false // leave Jasmine Spec Runner output visible in browser
    },
    reporters: ['progress', 'kjhtml'],
    port: 9876,
    colors: true,
    logLevel: config.LOG_INFO,
    autoWatch: true,
    browsers: ['Chromium'],
    singleRun: false,
    restartOnFileChange: true,
    listenAddress: 'localhost',
    hostname: 'localhost'
  });
};

