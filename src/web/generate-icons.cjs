const sharp = require('sharp');
const path = require('path');
const fs = require('fs');

const iconsDir = path.join(__dirname, 'public/icons');
const svgPath = path.join(__dirname, 'public/icon.svg');

// Create icons directory if it doesn't exist
if (!fs.existsSync(iconsDir)) {
  fs.mkdirSync(iconsDir, { recursive: true });
}

async function generateIcons() {
  const sizes = [192, 512];

  for (const size of sizes) {
    await sharp(svgPath)
      .resize(size, size)
      .png()
      .toFile(path.join(iconsDir, `icon-${size}x${size}.png`));
    console.log(`Generated icon-${size}x${size}.png`);
  }

  console.log('Icon generation complete!');
}

generateIcons().catch((err) => {
  console.error('Failed to generate icons:', err);
  process.exit(1);
});
