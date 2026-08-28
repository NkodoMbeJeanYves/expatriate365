# CI/CD — GitHub Actions

Déploiement automatique sur le VPS à chaque push sur la branche `develop`.

---

## Prérequis

- Le VPS est déjà configuré (script `setup-vps-lws.sh` exécuté)
- Le code source est hébergé sur **GitHub**
- `deploy-api.sh` est présent sur le VPS (`/usr/local/bin/deploy-api.sh`)
- `deploy-frontend.sh` sera transféré automatiquement par le workflow

---

## Étape 1 — Générer une clé SSH dédiée

Depuis Git Bash (Windows), à la racine du projet :

```bash
ssh-keygen -t ed25519 -C "github-actions-school365" -f ~/.ssh/school365_deploy
# Ne pas mettre de passphrase — appuyer Entrée deux fois
```

Deux fichiers sont créés :

- `~/.ssh/school365_deploy` → **clé privée** (pour GitHub)
- `~/.ssh/school365_deploy.pub` → **clé publique** (pour le VPS)

---

## Étape 2 — Autoriser la clé sur le VPS

```bash
# Afficher la clé publique
cat ~/.ssh/school365_deploy.pub

# Se connecter au VPS et ajouter la clé
ssh root@school365hub.poweryoursaas.com
echo "COLLER_LA_CLE_PUBLIQUE_ICI" >> ~/.ssh/authorized_keys
```

Vérifier que ça fonctionne :

```bash
ssh -i ~/.ssh/school365_deploy root@school365hub.poweryoursaas.com "echo OK"
# Doit afficher : OK
```

---

## Étape 3 — Configurer les secrets GitHub

Dans votre repo GitHub → **Settings** → **Secrets and variables** → **Actions** → **New repository secret** :

| Nom           | Valeur                                                       |
| ------------- | ------------------------------------------------------------ |
| `VPS_HOST`    | `school365hub.poweryoursaas.com`                             |
| `VPS_USER`    | `root`                                                       |
| `VPS_SSH_KEY` | Contenu complet de `~/.ssh/school365_deploy` (la **privée**) |

Pour afficher la clé privée à copier :

```bash
cat ~/.ssh/school365_deploy
# Copier tout le contenu, y compris -----BEGIN et -----END
```

---

## Étape 4 — Vérifier le workflow

Le fichier `.github/workflows/deploy.yml` est déjà créé dans le projet.
Il déclenche deux jobs en parallèle à chaque push sur `develop` :

| Job               | Ce qu'il fait                                         |
| ----------------- | ----------------------------------------------------- |
| `deploy-backend`  | `dotnet publish` → zip → scp → `deploy-api.sh`        |
| `deploy-frontend` | `npm run build` → tar.gz → scp → `deploy-frontend.sh` |

---

## Utilisation au quotidien

```bash
# Travailler sur une feature
git checkout -b feature/ma-fonctionnalite

# Commiter et pousser
git add .
git commit -m "feat: description"
git push origin feature/ma-fonctionnalite

# Merger dans develop → déploiement automatique
git checkout develop
git merge feature/ma-fonctionnalite
git push origin develop
# → GitHub Actions se déclenche et déploie sur le VPS
```

---

## Suivre un déploiement

Dans GitHub → onglet **Actions** → cliquer sur le dernier run.

Les logs de chaque job sont visibles en temps réel.

---

## En cas d'échec

Si un job échoue, le déploiement est annulé et le VPS conserve la version précédente.

Commandes utiles sur le VPS pour diagnostiquer :

```bash
journalctl -u school365-api -n 50 --no-pager
systemctl status school365-api
tail -f /var/log/nginx/error.log
```
