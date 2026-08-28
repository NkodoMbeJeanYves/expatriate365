# Migration .NET 9 → .NET 10

**Projet :** expatriate365  
**Date de rédaction :** 2026-08-27  
**Prérequis :** .NET 10 SDK disponible (GA prévu novembre 2025 — vérifiez https://dotnet.microsoft.com/download)

---

## Vue d'ensemble

La migration est essentiellement mécanique : mise à jour du `TargetFramework` et des packages
versionnés `9.x`. Aucun changement d'architecture n'est requis. Le risque principal est la
**compatibilité de Pomelo** (EF Core MySQL), qui doit sortir sa version 10.x avant de pouvoir migrer.

Durée estimée : **30–60 minutes** hors tests.

---

## Étape 1 — Vérifier la disponibilité des packages

Avant de toucher au code, confirmez que les packages suivants ont une version `10.x` stable sur NuGet :

| Package | Version actuelle | À vérifier sur nuget.org |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.0.0 | → 10.x |
| `Microsoft.AspNetCore.OpenApi` | 9.0.0 | → 10.x |
| `Microsoft.EntityFrameworkCore` | 9.0.0 | → 10.x |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.0 | → 10.x |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.0.0 | → 10.x |
| `Pomelo.EntityFrameworkCore.MySql` | 9.0.0 | → 10.x (**bloquant**) |
| `Serilog.AspNetCore` | 9.0.0 | → compatible 10.x |

> **Si Pomelo n'a pas encore de version 10.x**, la migration doit être reportée.
> Pomelo suit généralement EF Core avec un délai de quelques semaines.

```bash
# Vérifier en ligne de commande
dotnet package search Pomelo.EntityFrameworkCore.MySql --take 5
dotnet package search Microsoft.EntityFrameworkCore --take 5
```

---

## Étape 2 — Installer le SDK .NET 10 en local

```bash
# Windows — via winget
winget install Microsoft.DotNet.SDK.10

# ou via le script officiel (Linux/macOS/WSL)
wget https://dot.net/v1/dotnet-install.sh
bash dotnet-install.sh --channel 10.0

# Vérifier
dotnet --version   # doit afficher 10.x.x
dotnet --list-sdks # les deux SDK (9 et 10) peuvent coexister
```

---

## Étape 3 — Mettre à jour les fichiers `.csproj`

### `server/server.csproj`

```diff
- <TargetFramework>net9.0</TargetFramework>
+ <TargetFramework>net10.0</TargetFramework>
```

```diff
- <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
+ <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />

- <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
+ <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />

- <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
+ <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />

- <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
+ <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0">

- <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="9.0.0" />
+ <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="10.0.0" />

- <PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
+ <PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />  ← vérifier si 10.x dispo, sinon laisser
```

### `server/server.Tests/server.Tests.csproj`

```diff
- <TargetFramework>net9.0</TargetFramework>
+ <TargetFramework>net10.0</TargetFramework>

- <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
+ <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
```

---

## Étape 4 — Restaurer et builder

```bash
# Depuis la racine du projet
dotnet restore server/server.csproj
dotnet build   server/server.csproj -c Release

dotnet restore server/server.Tests/server.Tests.csproj
dotnet build   server/server.Tests/server.Tests.csproj
```

Corriger les éventuels **warnings devenus erreurs** entre .NET 9 et 10 (APIs obsolètes, changements
de comportement). Consultez le guide officiel : https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10

---

## Étape 5 — Lancer les tests

```bash
dotnet test server/server.Tests/server.Tests.csproj -c Release
```

---

## Étape 6 — Vérifier les migrations EF Core

.NET 10 peut introduire de légères différences dans la génération de migrations.
Vérifiez qu'aucune migration fantôme n'est générée :

```bash
cd server
dotnet ef migrations list
# Si une migration inattendue apparaît, l'inspecter avant de l'appliquer
```

---

## Étape 7 — Mettre à jour le script de setup VPS

Dans `scripts/setup-vps-expatriate.sh`, la valeur par défaut du channel .NET est saisie
interactivement. Au moment du redéploiement, entrez `10.0` à la place de `9.0`.

Le script détecte la version déjà installée et n'installe que si la version majeure est
insuffisante — aucun autre changement n'est requis dans les scripts.

---

## Étape 8 — Mettre à jour les scripts de déploiement (optionnel)

Si vous souhaitez documenter la version cible dans les scripts locaux, mettez à jour le
commentaire en tête de `deploy-backend-expatriate.sh` :

```diff
- # Prérequis : dotnet 9.x installé localement
+ # Prérequis : dotnet 10.x installé localement
```

---

## Résumé des fichiers à modifier

| Fichier | Modification |
|---|---|
| `server/server.csproj` | `TargetFramework` + 5 packages `9.0.0` → `10.0.0` |
| `server/server.Tests/server.Tests.csproj` | `TargetFramework` + 1 package |
| `scripts/setup-vps-expatriate.sh` | Saisir `10.0` au prompt channel .NET |

---

## Points de vigilance

- **Pomelo** est le seul package potentiellement bloquant — il dépend de EF Core et sort
  généralement quelques semaines après la GA de .NET.
- **Serilog.AspNetCore** versionné `9.x` reste compatible .NET 10 (pas lié au runtime) ;
  une mise à jour n'est pas obligatoire.
- **.NET 10 est une version STS** (Standard Term Support, 18 mois) — si vous préférez une
  version LTS, attendez .NET 12 (novembre 2026).
- Les migrations EF Core existantes **n'ont pas besoin d'être regénérées** — elles sont
  indépendantes de la version .NET.
