# Expatriate365

Plateforme web de gestion pour les associations et communautés expatriées. Elle regroupe la gestion des membres, des contributions et paiements, des finances, des événements, des réunions, de la gouvernance et des communications dans une application multi-tenant.

## Architecture

- `client/` : application web Angular standalone, avec PrimeNG, Tailwind CSS et `@ngx-translate`.
- `server/` : API ASP.NET Core, organisée par domaines fonctionnels, avec authentification JWT et autorisations par rôle/permission.
- `server/Infrastructure/Persistence/` et `server/Migrations/` : accès aux données via Entity Framework Core et migrations MySQL.
- `docker-compose.yml` : orchestration de MySQL, de l’API et du frontend servi par Nginx.
- `scripts/` : scripts de déploiement local et VPS.
- `conventions/` : conventions, spécifications et documentation de référence du projet.

### Technologies

Angular 22, TypeScript 6, PrimeNG 22, Tailwind CSS 3, ASP.NET Core 10, Entity Framework Core 9, MySQL 8, Nginx et Docker Compose.

## Prérequis

Pour le développement local :

- Node.js compatible avec Angular CLI 22 et npm 11 (la version npm du projet est définie dans `client/package.json`).
- SDK .NET 10.
- MySQL 8 accessible localement pour exécuter l’API.

Pour le lancement avec Docker : Docker Engine et le plugin Docker Compose.

## Démarrage local

### Frontend

Depuis la racine du dépôt :

```bash
cd client
npm ci
npm start
```

L’application est disponible sur <http://localhost:4200>. En développement, le frontend cible l’API à `http://localhost:5001`; cette adresse est définie dans `client/src/environments/environment.ts`.

### API

Configurez une base MySQL locale puis adaptez la chaîne `ConnectionStrings:MySql` dans `server/appsettings.json` ou dans une configuration locale non versionnée. Remplacez également la clé JWT de développement si nécessaire.

```bash
cd server
dotnet restore
dotnet run
```

L’API écoute par défaut sur <http://localhost:5001>. En environnement Development, la documentation OpenAPI et Scalar sont disponibles à <http://localhost:5001/scalar>. Au démarrage normal, l’API applique les migrations en attente et exécute le bootstrap initial; la base MySQL doit donc être accessible et les identifiants configurés correctement.

## Lancement avec Docker Compose

À la racine du dépôt, créez le fichier d’environnement puis remplacez toutes les valeurs d’exemple, en particulier les mots de passe et la clé JWT :

```bash
cp .env.example .env
```

Construisez le frontend et démarrez les services :

```bash
cd client
npm ci
npm run build
cd ..
docker compose up -d --build
```

L’application est servie sur le port indiqué par `HTTP_PORT` (par défaut `8080`, soit <http://localhost:8080>). Pour consulter les journaux :

```bash
docker compose logs -f api
docker compose logs -f frontend
```

Arrêt des services :

```bash
docker compose down
```

Les données MySQL et les fichiers téléversés sont conservés dans les volumes Docker `db_data` et `downloads`. Ne lancez `docker compose down -v` que si vous souhaitez aussi supprimer ces données.

## Tests et build

Tests frontend :

```bash
cd client
npm test
```

Build de production frontend :

```bash
cd client
npm run build
```

Tests backend :

```bash
dotnet test server/server.Tests/server.Tests.csproj
```

## Internationalisation (i18n)

Les fichiers de traduction se trouvent dans `client/public/i18n/` (`en.json`, `fr.json`).

### Validation manuelle

Depuis la racine du dépôt :

```bash
node scripts/validate-i18n.js
```

Ou depuis le dossier `client/` :

```bash
npm run lint:i18n
```

Ce script vérifie deux choses :

1. **Syntaxe JSON valide** — une erreur de syntaxe bloque le commit.
2. **Parité des clés** entre `en.json` et `fr.json` — les clés manquantes sont signalées en avertissement.

### Hook pre-commit automatique

Le hook Git `pre-commit` est inclus dans `scripts/hooks/pre-commit`. Il se déclenche automatiquement à chaque `git commit` **uniquement si des fichiers i18n sont stagés**, et bloque le commit en cas de JSON invalide.

**Installation du hook** (à faire une fois après le premier clone) :

```bash
node scripts/install-hooks.js
```

> Le dossier `.git/hooks/` n'est pas versionné. Sur cette machine, le hook est déjà installé. Chaque nouveau contributeur doit exécuter la commande ci-dessus après avoir cloné le dépôt.

### Ajouter une traduction

1. Ajouter la clé dans `client/public/i18n/en.json`.
2. Ajouter la traduction correspondante dans `client/public/i18n/fr.json`.
3. Vérifier : `node scripts/validate-i18n.js`.
4. Stager les deux fichiers ensemble.

## Configuration et sécurité

- `.env.example` est un modèle destiné à Docker Compose. Copiez-le vers `.env` et remplacez ses valeurs fictives avant tout démarrage.
- Ne versionnez jamais `.env`, de vrais secrets, des identifiants SMTP ou des clés de signature JWT.
- `server/appsettings.json` contient des valeurs de développement : elles ne doivent pas être réutilisées en production.
- Les modes `--reset` et `--seed` des scripts de déploiement peuvent effacer et recréer la base de données. Vérifiez la cible et disposez d’une sauvegarde avant de les utiliser.

## Déploiement

Les procédures opérationnelles détaillées sont documentées ici :

- [Guide général de déploiement](scripts/deployment-guide.md)
- [Guide de déploiement du backend](scripts/deploy-backend-guide.md)
- [Configuration de déploiement VPS](scripts/setup-vps-expatriate.sh)
- [Déploiement du backend](scripts/deploy-backend-expatriate.sh)
- [Déploiement du frontend](scripts/deploy-frontend-expatriate.sh)

Les scripts de déploiement VPS sont prévus pour l’environnement défini dans leurs paramètres. Vérifiez le domaine, les chemins et les secrets avant exécution.