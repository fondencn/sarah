const { exec } = require('child_process');
const https = require('https');
const fs = require('fs');
const path = require('path');

// Disable SSL certificate validation
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const url = 'https://localhost:7165/swagger/v1/swagger.json';
const outputFilePath = path.join(__dirname, 'swagger.json');

// Function to download the OpenAPI specification file
function downloadFile(url, dest, cb) {
  const file = fs.createWriteStream(dest);
  https.get(url, (response) => {
    if (response.statusCode !== 200) {
      cb(new Error(`Failed to get '${url}' (${response.statusCode})`));
      return;
    }
    response.pipe(file);
  });

  file.on('finish', () => {
    file.close(cb);
  });

  file.on('error', (err) => {
    fs.unlink(dest, () => cb(err));
  });
}

// Download the OpenAPI specification file
downloadFile(url, outputFilePath, (err) => {
  if (err) {
    console.error(`Error downloading file: ${err.message}`);
    return;
  }

  // Command to run openapi-generator-cli
  const command = `openapi-generator-cli generate -i ${outputFilePath} -g typescript-angular -o ./src/app/services/api-client`;

  exec(command, (error, stdout, stderr) => {
    if (error) {
      console.error(`Error: ${error.message}`);
      return;
    }
    if (stderr) {
      console.error(`Stderr: ${stderr}`);
      return;
    }
    console.log(`Stdout: ${stdout}`);
  });
});