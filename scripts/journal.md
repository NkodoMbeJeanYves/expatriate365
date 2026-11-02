# Consulter les journaux de l'API

Le service systemd de l'API s'appelle `expatriate-api`.

## Suivre tous les journaux en direct

```bash
sudo journalctl -u expatriate-api -f -o cat
```

## Filtrer les requêtes API et les événements email

```bash
sudo journalctl -u expatriate-api -f -o cat | grep --line-buffered -E 'HTTP .* /api/|Email sent|Failed to send email|SMTP not configured'
```

Les requêtes affichent la méthode HTTP, le chemin, le code de réponse et la durée. Les emails affichent `Email sent`, `Failed to send email` ou `SMTP not configured`.

## Afficher les journaux des dix dernières minutes

```bash
sudo journalctl -u expatriate-api --since "10 minutes ago" --no-pager
```
