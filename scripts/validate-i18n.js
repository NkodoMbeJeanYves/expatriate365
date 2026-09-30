#!/usr/bin/env node
/**
 * Validates i18n JSON files:
 *  1. Valid JSON syntax
 *  2. Key parity between en.json and fr.json (missing keys reported as warnings)
 */

const fs = require('fs');
const path = require('path');

const ROOT   = path.resolve(__dirname, '..');
const I18N   = path.join(ROOT, 'client', 'public', 'i18n');
const FILES  = ['en.json', 'fr.json'];

let hasError = false;

// ── 1. Syntax validation ─────────────────────────────────────────────────────

const parsed = {};

for (const file of FILES) {
  const filePath = path.join(I18N, file);
  if (!fs.existsSync(filePath)) {
    console.error(`✗  MISSING  ${file}`);
    hasError = true;
    continue;
  }
  try {
    parsed[file] = JSON.parse(fs.readFileSync(filePath, 'utf8'));
    console.log(`✓  OK       ${file}`);
  } catch (e) {
    console.error(`✗  INVALID  ${file} — ${e.message}`);
    hasError = true;
  }
}

if (hasError) {
  console.error('\n❌  i18n validation failed — fix JSON errors before committing.\n');
  process.exit(1);
}

// ── 2. Key parity check (en ↔ fr) ────────────────────────────────────────────

function flatKeys(obj, prefix = '') {
  const keys = [];
  for (const [k, v] of Object.entries(obj)) {
    const full = prefix ? `${prefix}.${k}` : k;
    if (v && typeof v === 'object' && !Array.isArray(v)) {
      keys.push(...flatKeys(v, full));
    } else {
      keys.push(full);
    }
  }
  return keys;
}

const enKeys = new Set(flatKeys(parsed['en.json']));
const frKeys = new Set(flatKeys(parsed['fr.json']));

const missingInFr = [...enKeys].filter(k => !frKeys.has(k));
const missingInEn = [...frKeys].filter(k => !enKeys.has(k));

if (missingInFr.length) {
  console.warn(`\n⚠  Keys in en.json but MISSING in fr.json (${missingInFr.length}):`);
  missingInFr.forEach(k => console.warn(`     - ${k}`));
}

if (missingInEn.length) {
  console.warn(`\n⚠  Keys in fr.json but MISSING in en.json (${missingInEn.length}):`);
  missingInEn.forEach(k => console.warn(`     - ${k}`));
}

if (missingInFr.length || missingInEn.length) {
  console.warn('\n⚠  Key parity warning — consider adding missing translations.\n');
} else {
  console.log('\n✓  Key parity OK — en.json and fr.json are in sync.\n');
}

process.exit(0);
