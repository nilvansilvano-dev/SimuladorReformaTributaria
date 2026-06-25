'use strict';

const JavaScriptObfuscator = require('javascript-obfuscator');
const fs   = require('fs');
const path = require('path');

const DIST = path.join(__dirname, 'dist');

// ── Criar pasta dist ─────────────────────────────────────────────
if (!fs.existsSync(DIST)) fs.mkdirSync(DIST);

// ── Copiar HTML e CSS sem alteração ─────────────────────────────
fs.copyFileSync(
  path.join(__dirname, 'index.html'),
  path.join(DIST, 'index.html')
);
fs.copyFileSync(
  path.join(__dirname, 'style.css'),
  path.join(DIST, 'style.css')
);
console.log('✓  index.html e style.css copiados');

// ── Ofuscar app.js ──────────────────────────────────────────────
const source = fs.readFileSync(path.join(__dirname, 'app.js'), 'utf8');

const result = JavaScriptObfuscator.obfuscate(source, {
  compact:                        true,
  controlFlowFlattening:          true,
  controlFlowFlatteningThreshold: 0.5,
  deadCodeInjection:              false,
  identifierNamesGenerator:       'hexadecimal',
  renameGlobals:                  false,
  rotateStringArray:              true,
  shuffleStringArray:             true,
  splitStrings:                   true,
  splitStringsChunkLength:        8,
  stringArray:                    true,
  stringArrayEncoding:            ['base64'],
  stringArrayThreshold:           0.8,
  transformObjectKeys:            true,
  unicodeEscapeSequence:          false,
});

fs.writeFileSync(path.join(DIST, 'app.js'), result.getObfuscatedCode());
console.log('✓  app.js ofuscado');

console.log('\n✅ Build concluído!');
console.log('   Faça deploy da pasta  frontend/dist/  no seu servidor.\n');
