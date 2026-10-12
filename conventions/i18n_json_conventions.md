# Expatriate365 — i18n JSON File Conventions

**Version:** 1.0  
**Date:** 2026-10-12  
**Applies to:** `client/public/i18n/en.json` and `client/public/i18n/fr.json`

---

## 1. File structure

- Two files: `en.json` (reference) and `fr.json` (translation).
- Flat sections per feature domain using dot-notation keys: `"<section>.<key>"`.
- Both files must stay in sync — the project's i18n validation hook (`PostToolUse:Edit`) runs automatically on every staged change and reports key-parity errors.

---

## 2. Encoding rules — critical

### 2.1 Always write files as UTF-8 without BOM

**Never use PowerShell `Set-Content` or `Out-File` to write i18n files.** PowerShell 5.1 defaults to UTF-8 **with BOM** (`\xEF\xBB\xBF`), which causes `SyntaxError: Unexpected token '﻿'` in Node.js JSON parsers.

Safe alternatives:
- **Edit tool** (Claude Code) — always writes UTF-8 no-BOM.
- **Node.js** `fs.writeFileSync(path, content, 'utf8')` — no BOM.
- **VS Code** — check status bar shows `UTF-8` (not `UTF-8 with BOM`).

To strip an accidental BOM with Node.js:
```js
let raw = fs.readFileSync(path, 'utf8');
if (raw.charCodeAt(0) === 0xFEFF) raw = raw.slice(1);
fs.writeFileSync(path, raw, 'utf8');
```

### 2.2 Never copy-paste text from Word / PDF / Windows apps

Copy-pasting from Word, Outlook, or PDF viewers is the primary source of **mojibake** — UTF-8 bytes misread as Latin-1. The corrupted sequences look like:

| Corrupted | Correct | Character |
|---|---|---|
| `â€"` / `â€”` | `—` / `—` | em dash |
| `â€¦` / `â€¦` | `…` / `…` | ellipsis |
| `ðŸ'‹` / `ðŸ‘‹` | `👋` / `👋` | wave emoji |

### 2.3 Emojis — use Unicode escapes or direct UTF-8 chars

Both forms are valid in JSON:
- Direct: `"greeting": "Hello {{name}} 👋"` — clean if the editor saves UTF-8.
- Escape: `"greeting": "Hello {{name}} 👋"` — always safe, independent of editor encoding.

**Prefer `\uXXXX` escapes for emojis and special punctuation** (`—`, `…`) to avoid encoding accidents.

### 2.4 Detecting corruption

Run this Node.js snippet to find mojibake lines:
```js
const fs = require('fs');
let raw = fs.readFileSync('client/public/i18n/en.json', 'utf8');
if (raw.charCodeAt(0) === 0xFEFF) raw = raw.slice(1);
raw.split('\n').forEach((line, i) => {
  if (/[â][€]/.test(line) || /[ð][Ÿ]/.test(line))
    console.log(`Line ${i+1}: ${line.trim()}`);
});
```

### 2.5 Fixing corruption

```js
const fs = require('fs');
function fix(path) {
  let raw = fs.readFileSync(path, 'utf8');
  if (raw.charCodeAt(0) === 0xFEFF) raw = raw.slice(1);
  raw = raw.split('â€”').join('—');   // — em dash
  raw = raw.split('â€¦').join('…');   // … ellipsis
  raw = raw.split('ðŸ‘‹').join('👋'); // 👋
  fs.writeFileSync(path, raw, 'utf8');
}
fix('client/public/i18n/en.json');
fix('client/public/i18n/fr.json');
```

> **Note:** The Grep tool may show false positives for `â` when searching. These are valid JSON Unicode escapes written literally in the file (e.g. `€` = `€`) — they decode correctly. Use Node.js to detect *actual* mojibake chars, not Grep.

---

## 3. Key naming

- Format: `<section>.<key>` — lowercase, underscores.
- Status values: `<section>.status_<value>` (e.g. `enrollment.status_draft`).
- Dynamic values: use `{{name}}` interpolation (ngx-translate syntax).
- No orphan keys — always add to both `en.json` and `fr.json` in the same commit.

---

## 4. Adding new keys

1. Add to `en.json` first (reference language).
2. Add the same key to `fr.json` with the French translation.
3. Stage both files together — the parity hook validates on `git add`.
4. Never add keys only to one file.

---

## 5. Key parity validation

The project has a git hook that runs automatically when i18n files are staged. It reports:
- Missing keys (present in `en.json` but absent from `fr.json` or vice versa).
- JSON syntax errors.

If the hook fails, fix the issue before committing.

---

## 6. Sections in use (2026-10)

```
nav, auth, common, errors, profile, community, roles,
members, member_dashboard, contributions, payments, finances,
events, meetings, welfare, elections, governance, documents,
notifications, announcements, audit, admin, countries
```

New modules must add a new section matching the Angular feature folder name.
