const fs = require('fs');
const dotenv = require('dotenv');

dotenv.config();

const environmentFilePath = './src/environments/environment.ts';
const environmentProdFilePath = './src/environments/environment.prod.ts';

const replaceEnvVars = (filePath) => {
  let content = fs.readFileSync(filePath, 'utf8');
  content = content.replace(/BING_MAPS_KEY_PLACEHOLDER/g, process.env.BING_MAPS_KEY);
  fs.writeFileSync(filePath, content, 'utf8');
  console.log(`Replaced BING_MAPS_KEY in ${filePath}`);
};

replaceEnvVars(environmentFilePath);
replaceEnvVars(environmentProdFilePath);