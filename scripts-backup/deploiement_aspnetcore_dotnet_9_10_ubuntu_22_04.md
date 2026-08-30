# Déploiement de deux applications ASP.NET Core (.NET 9 et .NET 10) sur Ubuntu 22.04

## 1. Objectif

Ce guide décrit la préparation d'un VPS **Ubuntu 22.04 LTS** pour héberger simultanément deux applications ASP.NET Core utilisant des versions différentes de .NET :

- une application sous **.NET 9** ;
- une application sous **.NET 10** ;
- **Nginx** comme reverse proxy ;
- **systemd** pour gérer les applications comme des services Linux ;
- **Let's Encrypt / Certbot** pour activer HTTPS.

Les différentes versions du SDK et du runtime .NET peuvent être installées côte à côte sur le même serveur.

---

## 2. Mettre Ubuntu à jour

```bash
sudo apt update
sudo apt upgrade -y
```

Installer les utilitaires nécessaires :

```bash
sudo apt install software-properties-common curl wget unzip -y
```

---

## 3. Ajouter le dépôt .NET Backports

Sur Ubuntu 22.04, .NET 9 et .NET 10 sont disponibles via le dépôt .NET Backports d'Ubuntu.

```bash
sudo add-apt-repository ppa:dotnet/backports
sudo apt update
```

---

## 4. Installer .NET 9 et .NET 10

### Option A : installer les SDK

Cette option est utile si le VPS doit aussi compiler ou publier les applications.

```bash
sudo apt install --install-suggests dotnet-sdk-9.0 -y
sudo apt install --install-suggests dotnet-sdk-10.0 -y
```

Vérifier les SDK installés :

```bash
dotnet --list-sdks
```

### Option B : installer uniquement les runtimes ASP.NET Core

Cette option est généralement suffisante si les applications sont compilées et publiées depuis une machine de développement ou une pipeline CI/CD.

```bash
sudo apt install --install-suggests aspnetcore-runtime-9.0 -y
sudo apt install --install-suggests aspnetcore-runtime-10.0 -y
```

Vérifier les runtimes installés :

```bash
dotnet --list-runtimes
```

Afficher les informations générales de l'installation :

```bash
dotnet --info
```

> Pour un VPS de production, privilégier les runtimes ASP.NET Core si aucune compilation ne sera effectuée sur le serveur. Cela limite les composants installés.

---

## 5. Installer Nginx

```bash
sudo apt install nginx -y
sudo systemctl enable nginx
sudo systemctl start nginx
```

Vérifier son état :

```bash
sudo systemctl status nginx
```

Si le pare-feu UFW est actif :

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
sudo ufw status
```

---

## 6. Créer un utilisateur de déploiement

Il est préférable de ne pas exécuter les applications avec le compte `root`.

```bash
sudo adduser deploy
sudo usermod -aG www-data deploy
```

Créer les dossiers des applications :

```bash
sudo mkdir -p /var/www/school365
sudo mkdir -p /var/www/ats365
```

Attribuer les permissions :

```bash
sudo chown -R deploy:www-data /var/www/school365
sudo chown -R deploy:www-data /var/www/ats365
sudo chmod -R 750 /var/www/school365
sudo chmod -R 750 /var/www/ats365
```

Architecture proposée :

```text
/var/www/
├── school365/
│   └── publish/
└── ats365/
    └── publish/
```

---

## 7. Publier les applications

### Application .NET 9

Depuis le dossier du projet :

```bash
dotnet publish -c Release -f net9.0 -o ./publish
```

### Application .NET 10

```bash
dotnet publish -c Release -f net10.0 -o ./publish
```

Copier ensuite les fichiers publiés vers le VPS, par exemple avec `scp` :

```bash
scp -r ./publish/* deploy@ADRESSE_IP_VPS:/var/www/school365/publish/
```

Pour la seconde application :

```bash
scp -r ./publish/* deploy@ADRESSE_IP_VPS:/var/www/ats365/publish/
```

Remplacer `ADRESSE_IP_VPS` par l'adresse IP réelle du serveur.

---

## 8. Configurer les ports des applications

Chaque application doit écouter sur un port local différent.

Exemple :

- School365 : `127.0.0.1:5001` ;
- ATS365 : `127.0.0.1:5002`.

Les ports seront définis directement dans les services systemd via la variable `ASPNETCORE_URLS`. Il n'est donc pas nécessaire d'exposer ces ports publiquement.

---

## 9. Créer le service systemd de School365 (.NET 9)

Créer le fichier :

```bash
sudo nano /etc/systemd/system/school365.service
```

Contenu :

```ini
[Unit]
Description=School365 ASP.NET Core Application
After=network.target

[Service]
WorkingDirectory=/var/www/school365/publish
ExecStart=/usr/bin/dotnet /var/www/school365/publish/School365.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=school365
User=www-data
Group=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5001

[Install]
WantedBy=multi-user.target
```

Remplacer `School365.Api.dll` par le nom réel du fichier DLL principal.

---

## 10. Créer le service systemd de l'application .NET 10

Créer le fichier :

```bash
sudo nano /etc/systemd/system/ats365.service
```

Contenu :

```ini
[Unit]
Description=ATS365 ASP.NET Core Application
After=network.target

[Service]
WorkingDirectory=/var/www/ats365/publish
ExecStart=/usr/bin/dotnet /var/www/ats365/publish/ATS.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=ats365
User=www-data
Group=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5002

[Install]
WantedBy=multi-user.target
```

Remplacer `ATS.Api.dll` par le nom réel du fichier DLL principal.

---

## 11. Activer et démarrer les services

Corriger d'abord la propriété des fichiers déployés si nécessaire :

```bash
sudo chown -R www-data:www-data /var/www/school365/publish
sudo chown -R www-data:www-data /var/www/ats365/publish
```

Recharger systemd :

```bash
sudo systemctl daemon-reload
```

Activer les services au démarrage du VPS :

```bash
sudo systemctl enable school365
sudo systemctl enable ats365
```

Démarrer les applications :

```bash
sudo systemctl start school365
sudo systemctl start ats365
```

Vérifier leur état :

```bash
sudo systemctl status school365
sudo systemctl status ats365
```

Consulter les journaux :

```bash
sudo journalctl -u school365 -f
```

```bash
sudo journalctl -u ats365 -f
```

Tester localement les applications depuis le VPS :

```bash
curl http://127.0.0.1:5001
curl http://127.0.0.1:5002
```

---

## 12. Configurer Nginx pour School365

Créer le fichier :

```bash
sudo nano /etc/nginx/sites-available/school365
```

Contenu :

```nginx
server {
    listen 80;
    listen [::]:80;

    server_name school365.app www.school365.app;

    location / {
        proxy_pass http://127.0.0.1:5001;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Activer le site :

```bash
sudo ln -s /etc/nginx/sites-available/school365 /etc/nginx/sites-enabled/school365
```

---

## 13. Configurer Nginx pour ATS365

Créer le fichier :

```bash
sudo nano /etc/nginx/sites-available/ats365
```

Contenu :

```nginx
server {
    listen 80;
    listen [::]:80;

    server_name ats365.app www.ats365.app;

    location / {
        proxy_pass http://127.0.0.1:5002;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Activer le site :

```bash
sudo ln -s /etc/nginx/sites-available/ats365 /etc/nginx/sites-enabled/ats365
```

Tester la configuration Nginx :

```bash
sudo nginx -t
```

Recharger Nginx :

```bash
sudo systemctl reload nginx
```

---

## 14. Configurer les DNS

Dans la zone DNS du fournisseur de domaine, créer les enregistrements de type `A` suivants :

```text
school365.app      -> ADRESSE_IP_VPS
www.school365.app  -> ADRESSE_IP_VPS
ats365.app         -> ADRESSE_IP_VPS
www.ats365.app     -> ADRESSE_IP_VPS
```

Si les applications utilisent des sous-domaines, le même principe s'applique :

```text
school.example.com -> ADRESSE_IP_VPS
ats.example.com    -> ADRESSE_IP_VPS
```

Nginx redirigera ensuite chaque domaine ou sous-domaine vers le port interne correspondant.

---

## 15. Installer HTTPS avec Certbot

Installer Certbot et son intégration Nginx :

```bash
sudo apt install certbot python3-certbot-nginx -y
```

Créer le certificat de School365 :

```bash
sudo certbot --nginx -d school365.app -d www.school365.app
```

Créer le certificat d'ATS365 :

```bash
sudo certbot --nginx -d ats365.app -d www.ats365.app
```

Tester le renouvellement automatique :

```bash
sudo certbot renew --dry-run
```

---

## 16. Variables d'environnement et secrets

Éviter de conserver les mots de passe de base de données ou les clés secrètes directement dans les fichiers suivis par Git.

Une approche consiste à créer un fichier d'environnement protégé pour chaque application.

Exemple :

```bash
sudo mkdir -p /etc/school365
sudo nano /etc/school365/school365.env
```

Contenu possible :

```text
ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=school365;User=school365_user;Password=CHANGE_ME;
Jwt__Key=CHANGE_ME_WITH_A_LONG_RANDOM_SECRET
```

Sécuriser le fichier :

```bash
sudo chown root:www-data /etc/school365/school365.env
sudo chmod 640 /etc/school365/school365.env
```

Ajouter ensuite cette ligne dans la section `[Service]` du service systemd :

```ini
EnvironmentFile=/etc/school365/school365.env
```

Après toute modification d'un service :

```bash
sudo systemctl daemon-reload
sudo systemctl restart school365
```

Appliquer le même principe à la seconde application.

---

## 17. Procédure de mise à jour d'une application

Exemple pour School365 :

```bash
sudo systemctl stop school365
```

Sauvegarder éventuellement la publication précédente :

```bash
sudo cp -a /var/www/school365/publish /var/www/school365/publish.backup
```

Copier la nouvelle version, puis corriger ses permissions :

```bash
sudo chown -R www-data:www-data /var/www/school365/publish
sudo chmod -R 750 /var/www/school365/publish
```

Redémarrer et vérifier :

```bash
sudo systemctl start school365
sudo systemctl status school365
sudo journalctl -u school365 -n 100 --no-pager
```

La même procédure peut être appliquée à `ats365`.

---

## 18. Architecture finale

```text
Internet
   │
   ├── school365.app
   │         │
   │         ▼
   │      Nginx
   │         │
   │         ▼
   │   127.0.0.1:5001
   │         │
   │         ▼
   │   School365 (.NET 9)
   │
   └── ats365.app
             │
             ▼
          Nginx
             │
             ▼
       127.0.0.1:5002
             │
             ▼
       ATS365 (.NET 10)
```

Organisation du VPS :

```text
Ubuntu 22.04 LTS
├── ASP.NET Core Runtime 9.0
├── ASP.NET Core Runtime 10.0
├── Nginx
├── Certbot
├── systemd
├── /var/www/school365/publish
└── /var/www/ats365/publish
```

---

## 19. Checklist de validation

- [ ] Ubuntu 22.04 est à jour.
- [ ] Le dépôt .NET Backports est configuré.
- [ ] Le runtime ou SDK .NET 9 est installé.
- [ ] Le runtime ou SDK .NET 10 est installé.
- [ ] `dotnet --list-runtimes` affiche les deux versions requises.
- [ ] Nginx est installé et actif.
- [ ] Chaque application possède son propre dossier.
- [ ] Chaque application possède son propre service systemd.
- [ ] Chaque application écoute uniquement sur `127.0.0.1` avec un port différent.
- [ ] Les services démarrent automatiquement avec le VPS.
- [ ] Les domaines ou sous-domaines pointent vers l'adresse IP du VPS.
- [ ] La configuration Nginx est valide.
- [ ] HTTPS est activé avec Certbot.
- [ ] Les secrets sont stockés hors du code source.
- [ ] Les journaux systemd ont été vérifiés.

---

## Références

- [Installer .NET sur Ubuntu](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install)
- [Guide de décision .NET et Ubuntu](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-decision)
- [Héberger ASP.NET Core sur Linux avec Nginx](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx)
