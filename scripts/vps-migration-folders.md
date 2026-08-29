# VPS Migration — Renommage des dossiers statiques

**Contexte :** Les dossiers `uploads`, `logos`, `photos`, `documents` ont été renommés
pour éviter les conflits avec d'autres applications sur le même VPS.

| Ancien | Nouveau |
|---|---|
| `/uploads/` | `/attachments/` |
| `/logos/` | `/branding/` |
| `/photos/` | `/avatars/` |
| `/documents/` | `/docs/` |
| `POST /api/v1/upload` | `POST /api/v1/attachments` |

---

## Étape 1 — Déployer le backend et le frontend

```bash
# Depuis la machine locale
bash scripts/deploy-backend-expatriate.sh
bash scripts/deploy-frontend-expatriate.sh
```

---

## Étape 2 — Créer les nouveaux dossiers et migrer les fichiers

```bash
# Créer les nouveaux dossiers
mkdir -p /var/www/expatriate365/api/wwwroot/{attachments,branding,avatars,docs}

# Migrer les fichiers existants
mv /var/www/expatriate365/api/wwwroot/uploads/*   /var/www/expatriate365/api/wwwroot/attachments/ 2>/dev/null || true
mv /var/www/expatriate365/api/wwwroot/logos/*     /var/www/expatriate365/api/wwwroot/branding/    2>/dev/null || true
mv /var/www/expatriate365/api/wwwroot/photos/*    /var/www/expatriate365/api/wwwroot/avatars/     2>/dev/null || true
mv /var/www/expatriate365/api/wwwroot/documents/* /var/www/expatriate365/api/wwwroot/docs/        2>/dev/null || true

# Corriger les permissions
chown -R expatriate365:expatriate365 /var/www/expatriate365/api/wwwroot/
```

---

## Étape 3 — Mettre à jour les URLs en base de données

Se connecter à MySQL puis exécuter :

```bash
mysql -u expatriate_user -p expatriate365
```

```sql
UPDATE tenants
    SET logo_url = REPLACE(logo_url, '/logos/', '/branding/')
    WHERE logo_url LIKE '%/logos/%';

UPDATE members
    SET photo_url = REPLACE(photo_url, '/photos/', '/avatars/')
    WHERE photo_url LIKE '%/photos/%';

UPDATE documents
    SET file_url = REPLACE(file_url, '/documents/', '/docs/')
    WHERE file_url LIKE '%/documents/%';

UPDATE payments
    SET receipt_file_url = REPLACE(receipt_file_url, '/uploads/', '/attachments/')
    WHERE receipt_file_url LIKE '%/uploads/%';

-- Vérification
SELECT logo_url FROM tenants WHERE logo_url IS NOT NULL LIMIT 5;
SELECT photo_url FROM members WHERE photo_url IS NOT NULL LIMIT 5;
SELECT file_url FROM documents LIMIT 5;
SELECT receipt_file_url FROM payments WHERE receipt_file_url IS NOT NULL LIMIT 5;
```

---

## Étape 4 — Mettre à jour la config Nginx

Éditer `/etc/nginx/sites-available/expatriate365` et remplacer les anciens blocs `location` :

```nginx
# Remplacer ceci :
location /uploads/   { alias /var/www/expatriate365/api/wwwroot/uploads/;   ... }
location /logos/     { alias /var/www/expatriate365/api/wwwroot/logos/;     ... }
location /photos/    { alias /var/www/expatriate365/api/wwwroot/photos/;    ... }
location /documents/ { alias /var/www/expatriate365/api/wwwroot/documents/; ... }

# Par ceci :
location /attachments/ {
    alias /var/www/expatriate365/api/wwwroot/attachments/;
    expires 30d;
    add_header Cache-Control "public, immutable";
}
location /branding/ {
    alias /var/www/expatriate365/api/wwwroot/branding/;
    expires 30d;
    add_header Cache-Control "public, immutable";
}
location /avatars/ {
    alias /var/www/expatriate365/api/wwwroot/avatars/;
    expires 30d;
    add_header Cache-Control "public, immutable";
}
location /docs/ {
    alias /var/www/expatriate365/api/wwwroot/docs/;
    expires 7d;
    add_header Cache-Control "public";
}
```

Puis recharger Nginx :

```bash
nginx -t && systemctl reload nginx
```

---

## Étape 5 — Redémarrer le backend et vérifier

```bash
systemctl restart expatriate365-api
systemctl status expatriate365-api
journalctl -u expatriate365-api -n 30 --no-pager
```

Tester un upload :

```bash
curl -s -X POST "https://ton-domaine.com/api/v1/attachments?folder=branding" \
  -H "Authorization: Bearer TON_TOKEN" \
  -F "file=@/tmp/test.png" | jq .
```

La réponse `file_url` doit contenir `/branding/`.
