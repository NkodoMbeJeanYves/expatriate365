# Audit Expatriate365 — Octobre 2026

## Ce qui est solide (ne pas toucher)

Auth, Membres, Cotisations, Paiements, Événements, Élections, Gouvernance, Finances, Analytics, Profil — backend et frontend complets et cohérents.

---

## Améliorations à fort impact (quick wins)

### Étape 1 — Notifications cliquables
Cliquer une notification ne redirige pas vers l'objet concerné.  
**Action :** Ajouter le routing depuis la notification vers l'entité liée.

### Étape 2 — Vue membre : mes demandes welfare
Un membre ne peut pas voir ses propres demandes welfare.  
**Action :** Créer endpoint `GET /welfare/me` + page `/my-welfare` Angular.

### Étape 3 — Exports manquants
Permissions définies mais aucun endpoint ni UI :
- Export CSV événements (`EventsExport`)
- Export CSV élections (`VotesExport`)
- Export CSV paiements (`PaymentsExport`)

**Action :** Implémenter les 3 endpoints backend + boutons d'export dans les pages correspondantes.

### Étape 4 — Hardcoding à externaliser
- Liste des pays : 9 pays seulement → liste ISO complète
- Devises : 8 options → liste étendue
- Types de welfare : constante frontend → configurable depuis les settings
- Messages de confirmation dans `governance.page.ts` non traduits → passer par i18n

---

## Fonctionnalités manquantes

### Étape 5 — Suppression de membre
Permission `MembersDelete` définie, aucun endpoint DELETE ni UI.  
**Action :** Endpoint `DELETE /members/{id}` (soft delete) + bouton de suppression dans la liste membres.

### Étape 6 — Vue calendrier événements et réunions
Tout est affiché en tableau.  
**Action :** Ajouter une vue calendrier (PrimeNG `p-calendar` ou FullCalendar) sur les pages événements et réunions.

### Étape 7 — Notifications temps réel
Actuellement en polling.  
**Action :** Intégrer WebSocket/SignalR ou Server-Sent Events pour remplacer le polling.

### Étape 8 — Directory (fonctionnalité orpheline)
`DirectoryEndpoints.cs` + `directory.page.ts` existent mais ne sont pas dans la nav.  
**Action :** Brancher dans la navigation ou supprimer si inutile.

---

## Incohérences à corriger

### Étape 9 — Permissions welfare et meetings mal câblées
- Welfare utilise `contributions.*` au lieu de permissions propres
- Meetings utilise `events.*`
Rend la configuration des rôles ambiguë pour l'`org_admin`.  
**Action :** Créer les domaines `welfare` et `meetings` dans `Permissions.cs` + mettre à jour les endpoints + reseed.

### Étape 10 — Pagination manquante dans gouvernance
- 50 résolutions chargées en dur (pas de pagination)
- 500 membres chargés en dur pour le select (risque mémoire)  
**Action :** Ajouter pagination côté backend et frontend sur la page gouvernance.

---

## Tableau de suivi

| Étape | Domaine | Complexité | Statut |
|---|---|---|---|
| 1 | Notifications cliquables | Faible | ⬜ À faire |
| 2 | Vue membre welfare | Moyenne | ⬜ À faire |
| 3 | Exports CSV (événements, élections, paiements) | Moyenne | ⬜ À faire |
| 4 | Hardcoding (pays, devises, welfare types, i18n) | Faible | ⬜ À faire |
| 5 | Suppression de membre | Faible | ⬜ À faire |
| 6 | Vue calendrier | Moyenne | ⬜ À faire |
| 7 | Notifications temps réel | Élevée | ⬜ À faire |
| 8 | Directory | Faible | ⬜ À faire |
| 9 | Permissions welfare/meetings | Moyenne | ⬜ À faire |
| 10 | Pagination gouvernance | Faible | ⬜ À faire |
