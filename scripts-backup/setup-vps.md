setup-vps

# Transférer le script

scp scripts/setup-vps-lws.sh root@VOTRE_IP:/tmp/

# Se connecter et l'exécuter

ssh root@VOTRE_IP
sed -i 's/\r//' /tmp/setup-vps-lws.sh
sudo bash /tmp/setup-vps-lws.sh

# Prérequis avant de lancer le script :

- Le DNS de votre domaine doit pointer vers l'IP du VPS (propagation ≥ quelques minutes) — requis pour certbot.
- Avoir préparé un mot de passe SMTP valide (si Gmail : créez un App Password, pas votre mot de passe principal).
- Connexion SSH en root sur un Ubuntu 22.04 LTS.

---

## Pourquoi le DNS doit pointer vers le VPS (requis pour certbot)

Let's Encrypt / certbot doit prouver que vous contrôlez le domaine avant d'émettre un certificat SSL.
Il le fait via un **challenge HTTP** : il place un fichier sur votre VPS, puis contacte
`http://votre-domaine.fr/.well-known/acme-challenge/...` depuis Internet pour vérifier que ce fichier est accessible.
Pour que ça fonctionne, **le domaine doit résoudre vers l'IP de votre VPS** au moment où certbot s'exécute.

### Ce que vous devez faire concrètement

1. **Trouvez l'IP publique de votre VPS** (LWS vous la communique dans votre espace client, ex : `91.234.56.78`)

2. **Dans la zone DNS de votre domaine** (chez votre registrar : OVH, Gandi, LWS, etc.), créez/modifiez ces enregistrements :

   | Type | Nom   | Valeur         |
   | ---- | ----- | -------------- |
   | `A`  | `@`   | `91.234.56.78` |
   | `A`  | `www` | `91.234.56.78` |

3. **Attendez la propagation DNS** : quelques minutes en général, jusqu'à 24 h selon le registrar.

4. **Vérifiez avant de lancer le script** :

   ```bash
   nslookup school365.monecole.fr
   # ou
   dig school365.monecole.fr +short
   # Doit retourner l'IP de votre VPS
   ```

> **Si le DNS ne pointe pas encore vers le VPS quand certbot s'exécute**, il échouera et vous n'aurez pas de HTTPS.
> Le script vous posera la question _"Lancer certbot maintenant ?"_ — répondez `non` si le DNS n'est pas encore propagé,
> puis relancez certbot manuellement :
>
> ```bash
> certbot --nginx -d votre-domaine.fr -d www.votre-domaine.fr
> systemctl reload nginx
> ```

---

## Si le script s'arrête à l'étape 10 (nginx / SSL)

Nginx échoue au démarrage car le certificat SSL n'existe pas encore. Suivez ces étapes manuellement :

### 1. Activer un vhost HTTP-only temporaire

```bash
cat > /etc/nginx/sites-available/school365-tmp <<EOF
server {
    listen 80;
    server_name school365hub.poweryoursaas.com www.school365hub.poweryoursaas.com;
    location /.well-known/acme-challenge/ { root /var/www/certbot; }
    location / { return 301 https://\$host\$request_uri; }
}
EOF

ln -sf /etc/nginx/sites-available/school365-tmp /etc/nginx/sites-enabled/school365
nginx -t && systemctl restart nginx
```

### 2. Lancer certbot manuellement

> Vérifiez d'abord que le DNS est propagé : `dig VOTRE_DOMAINE +short` doit retourner l'IP du VPS.

```bash
certbot --nginx \
    -d school365hub.poweryoursaas.com \
    -d www.school365hub.poweryoursaas.com \
    --non-interactive --agree-tos \
    --email noreply@poweryoursaas.com \
    --redirect
```

### 3. Remettre le vhost complet (avec SSL)

```bash
ln -sf /etc/nginx/sites-available/school365 /etc/nginx/sites-enabled/school365
rm -f /etc/nginx/sites-available/school365-tmp
nginx -t && systemctl reload nginx
```

### 4. Vérifier le renouvellement automatique

```bash
certbot renew --dry-run
```

Le script peut ensuite être relancé (`bash /tmp/setup-vps-lws.sh`) — les étapes déjà réalisées seront ignorées sans erreur.

cat > /etc/nginx/sites-available/school365-tmp <<EOF
server {
listen 80;
server_name school365hub.poweryoursaas.com www.school365hub.poweryoursaas.com;
location /.well-known/acme-challenge/ { root /var/www/certbot; }
location / { return 301 https://\$host\$request_uri; }
}
EOF

ln -sf /etc/nginx/sites-available/school365-tmp /etc/nginx/sites-enabled/school365
nginx -t && systemctl restart nginx

DOMAIN="school365hub.poweryoursaas.com"
sed -i "s|/etc/letsencrypt/live//|/etc/letsencrypt/live/${DOMAIN}/|g" /etc/nginx/sites-available/school365
sed -i "s|server_name  |server_name ${DOMAIN} www.${DOMAIN}|g" /etc/nginx/sites-available/school365

grep -E "ssl_certificate|server_name" /etc/nginx/sites-available/school365

certbot --nginx -d school365hub.poweryoursaas.com -d www.school365hub.poweryoursaas.com \
 --non-interactive --agree-tos --email noreply@poweryoursaas.com --redirect

ln -sf /etc/nginx/sites-available/school365 /etc/nginx/sites-enabled/school365
rm -f /etc/nginx/sites-available/school365-tmp
nginx -t && systemctl reload nginx
