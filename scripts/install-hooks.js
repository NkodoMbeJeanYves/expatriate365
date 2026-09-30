#!/usr/bin/env node
/**
 * Installs Git hooks for this repository.
 * Run once after cloning: node scripts/install-hooks.js
 */

const fs   = require('fs');
const path = require('path');

const ROOT      = path.resolve(__dirname, '..');
const HOOKS_DIR = path.join(ROOT, '.git', 'hooks');
const SRC_DIR   = path.join(ROOT, 'scripts', 'hooks');

if (!fs.existsSync(SRC_DIR)) {
  console.log('No hooks to install.');
  process.exit(0);
}

const hooks = fs.readdirSync(SRC_DIR);
for (const hook of hooks) {
  const src  = path.join(SRC_DIR, hook);
  const dest = path.join(HOOKS_DIR, hook);
  fs.copyFileSync(src, dest);
  try { fs.chmodSync(dest, 0o755); } catch {}
  console.log(`✓  Installed hook: ${hook}`);
}

console.log('\nDone. Git hooks installed.\n');
