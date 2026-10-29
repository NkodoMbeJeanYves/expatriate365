# Audit Expatriate365 — Itération 2 (Octobre 2026)

> Étapes 1–10 du premier audit entièrement implémentées.  
> Ce rapport couvre les 15 nouvelles améliorations identifiées.

---

## Synthèse des priorités

| Priorité | Étapes | Thème principal |
|---|---|---|
| 🔴 Critique | 11, 12, 13 | Sécurité RBAC + stabilité stores + mémoire |
| 🟠 Importante | 14, 15, 16, 17, 18, 19, 20 | Sécurité secondaire + dette technique + UX |
| 🟡 Faible | 21, 22, 23, 24, 25 | Polissage et cohérence |

---

## 🔴 Priorité Critique

---

### Étape 11 — Endpoint `/roles/permissions` trop ouvert

**Impact :** Sécurité  
**Complexité :** Faible

**Problème :**  
L'endpoint `GET /api/v1/roles/permissions` retourne la liste complète de toutes les permissions du système. Il n'a aucun scope de permission explicite — uniquement le groupe générique `.RequireAuthorization()`. Tout utilisateur authentifié (y compris un simple membre) peut découvrir la totalité de la surface d'autorisation de l'application.

**Fichier :** `server/API/Roles/RoleEndpoints.cs`, lignes 22–31

**Correctif :**
```csharp
.RequireAuthorization(Permissions.RolesRead)
```

---

### Étape 12 — Mutations silencieuses dans 9 stores Angular

**Impact :** Stabilité + UX  
**Complexité :** Moyenne

**Problème :**  
Les actions de mutation (create, update, delete) dans 9 stores sont déclenchées avec `.subscribe()` nu, sans bloc `error:`. En cas d'échec réseau ou réponse 4xx/5xx, l'utilisateur ne reçoit aucun retour, le store reste dans un état incohérent, et l'exception est avalée silencieusement.

**Fichiers concernés :**
- `client/src/app/core/stores/communications.store.ts`
- `client/src/app/core/stores/welfare.store.ts`
- `client/src/app/core/stores/meetings.store.ts`
- `client/src/app/core/stores/events.store.ts`
- `client/src/app/core/stores/payments.store.ts`
- `client/src/app/core/stores/contributions.store.ts`
- `client/src/app/core/stores/documents.store.ts`
- `client/src/app/core/stores/elections.store.ts`
- `client/src/app/core/stores/admin.store.ts`

**Correctif :**  
Remplacer les `.subscribe()` nus par `{ next: ..., error: err => this.toast.error(...) }` ou centraliser via un opérateur RxJS `catchError` dans un service dédié.

---

### Étape 13 — Chargement massif pour les exports CSV (`limit: 10000`)

**Impact :** Performance + fiabilité  
**Complexité :** Moyenne

**Problème :**  
Trois appels chargent des milliers d'enregistrements en un seul bloc en mémoire navigateur pour alimenter des exports CSV ou des previews. Sur une association avec un grand historique financier, cela provoque timeout API, saturation mémoire et freeze UI.

**Fichiers :**
- `client/src/app/features/contributions/pages/contribution-list/contribution-list.page.ts`, ligne 609 : `getCharges(1, 1000)`
- `client/src/app/features/contributions/pages/contribution-list/contribution-list.page.ts`, ligne 628 : `getCharges(1, 10000)`
- `client/src/app/features/finances/pages/finances-page/finances.page.ts`, ligne 234 : `limit: 10000`

**Correctif :**  
Implémenter des endpoints d'export dédiés côté serveur retournant un `Stream` (fichier CSV généré server-side), comme déjà fait pour les exports événements/élections/paiements à l'étape 3.

---

## 🟠 Priorité Importante

---

### Étape 14 — `GET /welfare-requests/me` sans scope de permission

**Impact :** Sécurité  
**Complexité :** Faible

**Problème :**  
Seul endpoint welfare sans `.RequireAuthorization(Permissions.xxx)` explicite. Il repose uniquement sur la présence du claim `entity_id`. Un utilisateur authentifié sans rôle `member` mais avec un `entity_id` valide peut accéder aux données.

**Fichier :** `server/API/Welfare/WelfareEndpoints.cs`, lignes 27–37

**Correctif :**
```csharp
.RequireAuthorization(Permissions.WelfareReadOwn)
```

---

### Étape 15 — Upload sans isolation tenant ni quota

**Impact :** Sécurité + intégrité des données  
**Complexité :** Moyenne

**Problème :**  
L'endpoint `POST /api/v1/upload` vérifie l'authentification mais n'extrait pas le `tenant_id` du token. N'importe quel utilisateur authentifié peut uploader des fichiers dans n'importe quel dossier (`attachments`, `branding`, `avatars`, `docs`). Aucun quota par tenant, aucun nommage scopé — les fichiers sont stockés dans un répertoire partagé.

**Fichier :** `server/API/Upload/UploadEndpoints.cs`

**Correctif :**
- Préfixer le chemin de stockage avec le `tenant_id` extrait du token
- Ajouter un quota configurable par tenant
- Envisager un stockage objet (S3 / Azure Blob) à terme

---

### Étape 16 — Reçu de paiement HTML hardcodé en français

**Impact :** Internationalisation  
**Complexité :** Faible

**Problème :**  
Le template HTML du reçu (endpoint `GET /payments/{id}/receipt`) contient des strings comme `"Reçu de paiement"`, `"N° de reçu"`, `"Membre"`, `"Type de contribution"` — toutes hardcodées en français dans le C#. Non traduisible en l'état.

**Fichier :** `server/API/Payments/PaymentEndpoints.cs`, lignes 116–158

**Correctif :**  
Externaliser le template dans un fichier `.html` Razor ou un fichier de ressources, avec injection de variables de traduction par langue du tenant.

---

### Étape 17 — SuperAdmin : contrôle d'accès par comparaison de string

**Impact :** Sécurité  
**Complexité :** Faible

**Problème :**  
La fonction `IsSuperAdmin()` compare manuellement la valeur du claim `role` à la chaîne `"super_admin"`. En cas de changement du nom du rôle ou d'un claim mal formé, ce contrôle peut silencieusement tomber sans déclencher d'erreur d'autorisation formelle. Incohérent avec le reste de l'application qui utilise `RequireAuthorization(Permissions.xxx)` déclaratif.

**Fichier :** `server/API/SuperAdmin/SuperAdminEndpoints.cs`, lignes 114–119

**Correctif :**  
Créer une policy ASP.NET Core `"SuperAdmin"` dans `Program.cs` et remplacer les checks inline.

---

### Étape 18 — Couverture de tests très faible

**Impact :** Qualité / maintenabilité  
**Complexité :** Élevée

**Problème :**  
Seulement 3 fichiers de tests existent dans `server/server.Tests/` :
- `ChargeGenerationIdempotenceTests.cs`
- `ElectionHasVotedTests.cs`
- `RecordPaymentCommandTests.cs`

Aucun test pour : authentification et refresh de tokens, gestion des membres, welfare (approbation / rejet / paiement), gouvernance (mandats / résolutions), événements, communications, documents. Ces domaines ont des règles complexes (multi-tenant, machine à états, permissions) entièrement non vérifiées automatiquement.

**Correctif :**  
Ajouter des tests d'intégration par domaine, en priorité pour les commandes avec effets de bord : `WelfareCommand`, `PaymentCommand`, `EnrollmentCommand`, isolation multi-tenant.

---

### Étape 19 — `window.confirm()` natif dans 2 composants

**Impact :** UX + cohérence  
**Complexité :** Faible

**Problème :**  
Deux composants utilisent le dialogue bloquant natif du navigateur au lieu du `p-confirmdialog` PrimeNG déjà utilisé dans events, documents, governance, admin-user-list, member-list, etc. Brise la cohérence visuelle, ignore le thème de l'application, inaccessible.

**Fichiers :**
- `client/src/app/features/contributions/pages/contribution-list/contribution-list.page.ts`, ligne 594
- `client/src/app/features/members/pages/member-categories/member-categories.page.ts`, ligne 234

**Correctif :**  
Remplacer par `ConfirmationService` + `<p-confirmdialog />` comme dans les autres pages.

---

### Étape 20 — Strings françaises hardcodées dans les templates TypeScript

**Impact :** Internationalisation  
**Complexité :** Moyenne

**Problème :**  
Plusieurs composants contiennent des strings françaises inline dans le TypeScript, non extraites par `ngx-translate`. Elles passent à travers tout pipeline de traduction.

**Fichiers (sélection) :**
- `election-form-drawer.component.ts` : `"Modifier l'élection"`, `"Nouvelle élection"`
- `meeting-form-drawer.component.ts` : `"Enregistrer"`, `"Créer"`
- `event-form-drawer.component.ts`
- `meeting-action-items-drawer.component.ts`
- `community-post.page.ts`, ligne 130
- `contribution-list.page.ts`, lignes 308, 525

**Correctif :**  
Remplacer par `this.translate.instant('clé.i18n')` et ajouter les clés dans `en.json` / `fr.json`.

---

## 🟡 Priorité Faible

---

### Étape 21 — Analytics et Dashboard : subscribe sans error handler

**Impact :** UX  
**Complexité :** Faible

**Problème :**  
Les subscribes sur les appels de lecture de stats n'ont pas de bloc `error:`. Un échec API (token expiré, réseau coupé) laisse la page figée en état de chargement ou vide, sans message à l'utilisateur.

**Fichiers :**
- `client/src/app/features/analytics/pages/analytics-page/analytics.page.ts`, ligne 225
- `client/src/app/features/analytics/pages/dashboard/dashboard.page.ts`, lignes 311, 316
- `client/src/app/features/finances/pages/finances-page/finances.page.ts`, lignes 198, 216, 239

---

### Étape 22 — `GET /tenant/settings` accessible à tout utilisateur authentifié

**Impact :** Sécurité (mineure)  
**Complexité :** Faible

**Problème :**  
`MapGet("/settings")` n'a pas de scope explicite, contrairement au `MapPut` qui a `Permissions.SettingsUpdate`. Tout utilisateur authentifié peut lire les paramètres du tenant (nom, logo URL, devise, etc.).

**Fichier :** `server/API/Tenant/TenantEndpoints.cs`, lignes 30–37

**Correctif :**
```csharp
.RequireAuthorization(Permissions.SettingsRead)
```

---

### Étape 23 — `POST /communications/{id}/read` sans scope

**Impact :** Sécurité (mineure)  
**Complexité :** Faible

**Problème :**  
Tout utilisateur authentifié peut marquer n'importe quelle communication comme lue pour n'importe quel `memberId` reçu via claim, sans vérification de scope.

**Fichier :** `server/API/Communications/CommunicationEndpoints.cs`, ligne 72

**Correctif :**
```csharp
.RequireAuthorization(Permissions.AnnouncementsRead)
```

---

### Étape 24 — `member-categories.page.ts` potentiellement orphelin

**Impact :** Dead code  
**Complexité :** Faible

**Problème :**  
Ce composant utilise `confirm()` natif (étape 19) et n'apparaît peut-être dans aucune route de `MEMBERS_ROUTES`. Si la page n'est pas accessible, le composant est du dead code.

**Fichier :** `client/src/app/features/members/pages/member-categories/member-categories.page.ts`

**Action :** Vérifier la présence dans `app.routes.ts` → supprimer ou rattacher à une route.

---

### Étape 25 — Préférences notifications sans feedback utilisateur

**Impact :** UX  
**Complexité :** Faible

**Problème :**  
`this.api.updatePreferences(this.preferences()).subscribe()` est nu — ni toast de succès, ni gestion d'erreur. L'utilisateur pense que ses préférences ont été sauvegardées même en cas d'échec.

**Fichier :** `client/src/app/features/notifications/notifications.page.ts`, ligne 148

**Correctif :**  
Ajouter `{ next: () => this.toast.success(...), error: () => this.toast.error(...) }`.

---

## Tableau de suivi

| Étape | Domaine | Priorité | Complexité | Statut |
|---|---|---|---|---|
| 11 | RBAC — `/roles/permissions` trop ouvert | 🔴 Critique | Faible | ⬜ À faire |
| 12 | Mutations silencieuses (9 stores) | 🔴 Critique | Moyenne | ⬜ À faire |
| 13 | Exports CSV — `limit: 10000` | 🔴 Critique | Moyenne | ⬜ À faire |
| 14 | RBAC — `/welfare/me` sans scope | 🟠 Importante | Faible | ⬜ À faire |
| 15 | Upload sans isolation tenant | 🟠 Importante | Moyenne | ⬜ À faire |
| 16 | Reçu paiement HTML en dur | 🟠 Importante | Faible | ⬜ À faire |
| 17 | SuperAdmin — check de rôle fragile | 🟠 Importante | Faible | ⬜ À faire |
| 18 | Tests unitaires absents | 🟠 Importante | Élevée | ⬜ À faire |
| 19 | `window.confirm()` natif (2 composants) | 🟠 Importante | Faible | ⬜ À faire |
| 20 | Strings françaises hardcodées | 🟠 Importante | Moyenne | ⬜ À faire |
| 21 | Analytics/Dashboard — subscribe sans error | 🟡 Faible | Faible | ⬜ À faire |
| 22 | RBAC — `/tenant/settings` trop ouvert | 🟡 Faible | Faible | ⬜ À faire |
| 23 | RBAC — `/communications/{id}/read` | 🟡 Faible | Faible | ⬜ À faire |
| 24 | `member-categories.page.ts` orphelin | 🟡 Faible | Faible | ⬜ À faire |
| 25 | Préférences notifications sans feedback | 🟡 Faible | Faible | ⬜ À faire |
