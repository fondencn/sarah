#!/usr/bin/env node
// Generates PNG PWA icons from the SVG source icon.
// Run once: node generate-pwa-icons.js
const sharp = require('sharp');
const path = require('path');
const fs = require('fs');

const sizes = [72, 96, 128, 144, 152, 192, 384, 512];
const srcSvg = path.join(__dirname, 'src/assets/icons/icon-512x512.svg');
const outDir = path.join(__dirname, 'src/assets/icons');

(async () => {
  const svgBuffer = fs.readFileSync(srcSvg);
  for (const size of sizes) {
    const outFile = path.join(outDir, `icon-${size}x${size}.png`);
    await sharp(svgBuffer)
      .resize(size, size)
      .png()
      .toFile(outFile);
    console.log(`Generated ${outFile}`);
  }
  console.log('Done.');
})();
