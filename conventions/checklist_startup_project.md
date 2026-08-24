# Checklist de démarrage de projet Angular
## Basé sur les pratiques du projet TechNova / EV Charge

> Cochez chaque point avant de commencer le développement des features.  
> Ordre recommandé : suivre les étapes dans l'ordre numérique.

---

## PHASE 1 — Initialisation du projet

### 1.1 Création et configuration de base
- [ ] `ng new <nom-projet> --standalone --routing --style=css`
- [ ] Configurer `angular.json` : `changeDetection: OnPush` par défaut pour tous les composants
- [ ] Configurer `tsconfig.json` : `strict: true` + `noImplicitOverride`, `noImplicitReturns`, `noFallthroughCasesInSwitch`, `strictTemplates`
- [ ] Définir les path aliases TypeScript (`@core/*`, `@shared/*`, `@features/*`, `@layouts/*`, `@env/*`)
- [ ] Créer les deux fichiers d'environnement (`environment.ts` et `environment.development.ts`)
- [ ] Vérifier que `environment.development.ts` est bien en `fileReplacements` dans `angular.json`
- [ ] Installer les dépendances : `@ngrx/signals`, `@ngx-translate`, library UI (PrimeNG, etc.)

### 1.2 Structure de dossiers
- [ ] Créer `src/app/core/`
- [ ] Créer `src/app/shared/`
- [ ] Créer `src/app/features/`
- [ ] Créer `src/app/layouts/`
- [ ] Créer `src/environments/`
- [ ] Créer `public/i18n/fr.json` et `public/i18n/en.json` (au moins `{}`)

---

## PHASE 2 — Couche core (à faire en tout premier)

### 2.1 Configuration typée
- [ ] Créer `core/config/app-config.token.ts` avec l'interface `AppConfig` et `InjectionToken`
- [ ] Brancher `APP_CONFIG` sur `environment` via la factory
- [ ] Définir tous les champs de config nécessaires (`apiBaseUrl`, `useStaticData`, etc.)

### 2.2 API layer
- [ ] Créer `core/api/api-url.ts` avec la fonction `apiUrl(path)`
- [ ] Créer `core/api/api-types.ts` avec `Paged<T>`, `CursorPaged<T>`, `ApiError`, `extractApiError()`

### 2.3 Intercepteurs HTTP
- [ ] Créer `core/http/loading.service.ts` (signal counter)
- [ ] Créer `core/http/loading.interceptor.ts` (avec opt-out `X-Skip-Loading`)
- [ ] Créer `core/http/error.interceptor.ts` (401 → logout, 403 → forbidden, 5xx → toast)
- [ ] Créer `core/auth/auth.interceptor.ts` (ajout `Authorization: Bearer`)
- [ ] Enregistrer la chaîne dans `app.config.ts` : `[authInterceptor, loadingInterceptor, errorInterceptor]`

### 2.4 Authentification
- [ ] Définir les rôles dans `core/auth/models/role.model.ts` (`as const`, type union)
- [ ] Définir les interfaces dans `core/auth/models/user.model.ts` (`AuthUser`, `AuthTokens`, `AuthSession`)
- [ ] Créer `core/auth/auth.store.ts` avec persistance `localStorage`, `hasAnyRole()`, `patchCurrentUser()`
- [ ] Créer `core/auth/auth.service.ts` (`login()`, `logout()`, `refresh()`, `me()`, `bootstrap()`, `forceLogout()`)
- [ ] Créer `core/auth/auth.guard.ts` (`authGuard` + factory `hasRoleGuard(roles)`)
- [ ] Enregistrer `bootstrap()` dans `provideAppInitializer()`

### 2.5 i18n
- [ ] Créer `core/i18n/i18n.providers.ts` avec `provideI18n()` (chargement, langue stockée en localStorage)
- [ ] Enregistrer `provideI18n()` dans `app.config.ts`
- [ ] Définir les clés de base dans `fr.json` et `en.json` (nav, erreurs communes)

### 2.6 Theme (dark mode)
- [ ] Créer `core/theme/theme.service.ts` (signal, localStorage, `prefers-color-scheme`, classe CSS sur `<html>`)
- [ ] Enregistrer `init()` dans `provideAppInitializer()`

---

## PHASE 3 — Couche shared

### 3.1 Directive de rôle
- [ ] Créer `shared/directives/has-role.directive.ts` (`[appHasRole]`, `input.required`, `effect()`)

### 3.2 Pipes utilitaires (selon le domaine)
- [ ] Créer `shared/pipes/duration.pipe.ts` (null-safe, retourne `–` si null)
- [ ] Créer `shared/pipes/relative-time.pipe.ts` (accepte `now` en paramètre, i18n)
- [ ] Créer d'autres pipes métier si pertinent (énergie, monnaie, etc.)

### 3.3 Services partagés
- [ ] Créer `shared/services/time-ticker.service.ts` (signal horloge, `DestroyRef` pour le cleanup)

### 3.4 Utilitaires
- [ ] Créer `shared/utils/csv-export.ts` (`toCsv<T>()`, `downloadCsv()` avec BOM UTF-8)
- [ ] Créer les utilitaires de formatage date/timezone si besoin

### 3.5 Modèles partagés
- [ ] Définir les interfaces de base (`*.model.ts`) dans `shared/models/` pour les entités cross-features

---

## PHASE 4 — Layouts

### 4.1 Auth layout
- [ ] Créer `layouts/auth-layout/auth-layout.ts` (fond centré, logo, `<router-outlet>`)

### 4.2 Main layout
- [ ] Créer `layouts/main-layout/nav.config.ts` (groupes de nav comme données, clés i18n, rôles)
- [ ] Créer `layouts/main-layout/sidebar-state.service.ts` (collapsed signal + localStorage + media query mobile)
- [ ] Créer `layouts/main-layout/components/app-sidebar.ts` (itère sur `NAV_GROUPS`, filtre par rôle)
- [ ] Créer `layouts/main-layout/components/app-navbar.ts` (toggle sidebar, user menu, theme toggle)
- [ ] Créer `layouts/main-layout/main-layout.ts` (compose sidebar + navbar + `<router-outlet>`)

---

## PHASE 5 — Routing principal

### 5.1 Routes
- [ ] Configurer `app.routes.ts` :
  - [ ] Route racine → redirect vers dashboard
  - [ ] Zone publique `auth/` → `loadChildren` vers `auth.routes.ts`
  - [ ] Shell protégé : `canMatch: [authGuard]`, `loadComponent: MainLayout`, children
  - [ ] Routes admin : `canMatch: [hasRoleGuard(ADMIN_ONLY)]`
  - [ ] Route `forbidden` si nécessaire
- [ ] Activer `withComponentInputBinding()` et `withViewTransitions()` dans `provideRouter()`

---

## PHASE 6 — Feature auth (première feature)

- [ ] Créer `features/auth/pages/login/login.page.ts`
- [ ] Créer `features/auth/auth.routes.ts`
- [ ] Créer les données mock `features/auth/data/mock-accounts.ts`
- [ ] Tester le flux complet : login → session persistée → bootstrap → redirect → logout

---

## PHASE 7 — Design System (avant les features métier)

- [ ] Définir les classes `.ds-*` de layout (`ds-shell`, `ds-card`, `ds-stat`, `ds-metric`)
- [ ] Configurer le preset de couleurs (theme UI lib + Tailwind tokens)
- [ ] Créer une page `/design-system` qui showcasse tous les composants visuels réutilisables
- [ ] Documenter les règles CSS dans `AGENTS.md` (ne jamais hard-coder les couleurs de marque)

---

## PHASE 8 — Première feature métier (exemple : admin users)

Répéter ce bloc pour chaque feature :

- [ ] Créer `features/<feature>/api/<resource>.api.ts` (un service par ressource, `apiUrl()`, typages stricts)
- [ ] Créer `features/<feature>/state/<resource>.store.ts` (SignalStore, `withState` + `withComputed` + `withMethods`)
- [ ] Créer les données mock `features/<feature>/data/mock-<resource>.ts`
- [ ] Créer `features/<feature>/components/<feature>.types.ts` (types locaux à la feature)
- [ ] Créer les composants dumb (`*.component.ts`) : `input()` uniquement, `output()` pour les événements
- [ ] Créer la page smart (`*.page.ts`) : injecte le store, orchestre les actions, signaux locaux
- [ ] Créer `features/<feature>/<feature>.routes.ts` (lazy loading)
- [ ] Brancher la route dans `app.routes.ts`

---

## PHASE 9 — Temps réel (si applicable)

- [ ] Créer `core/realtime/realtime.gateway.ts` (WebSocket, reconnexion exponentielle, `Subject<Message>`, signal status)
- [ ] Définir les types de messages dans `core/realtime/<protocol>.types.ts`
- [ ] Brancher le gateway dans les stores concernés via `messages.pipe(filter(...))`
- [ ] Respecter la bascule `useStaticData` : gateway inactif si true

---

## PHASE 10 — Qualité et documentation

- [ ] Créer `AGENTS.md` à la racine du frontend (stack, couches, conventions, design system)
- [ ] Créer `docs/backend-contract.md` (contrat API : conventions, endpoints, erreurs, pagination)
- [ ] Configurer ESLint avec `angular-eslint`
- [ ] Vérifier que tous les composants sont `ChangeDetectionStrategy.OnPush`
- [ ] Vérifier qu'aucun composant dumb n'injecte un store
- [ ] Vérifier que les imports entre features sont absents (`@features/A` dans `@features/B`)
- [ ] Vérifier les alias TypeScript : plus aucun `../../..` dans les imports

---

## Vérifications finales avant livraison

### Architecture
- [ ] `core/` n'a pas de dépendance circulaire interne
- [ ] `shared/` ne contient pas de state (pas de store, pas de signal mutable exposé)
- [ ] Chaque feature est auto-contenue
- [ ] Les routes sont toutes lazy-loadées

### Qualité du code
- [ ] Tous les types d'état sont `readonly`
- [ ] `patchState()` est la seule façon de modifier l'état
- [ ] Toutes les erreurs API passent par `extractApiError()`
- [ ] Pas de `console.log()` en production
- [ ] `DestroyRef.onDestroy()` utilisé pour tous les `setInterval` et subscriptions non-AsyncPipe

### UX
- [ ] `LoadingService` branché sur le spinner global
- [ ] Erreurs 401 → redirection login automatique (intercepteur)
- [ ] Erreurs 5xx → toast d'erreur (intercepteur)
- [ ] Dark mode fonctionnel (respecte `prefers-color-scheme` par défaut)
- [ ] i18n : toutes les chaînes visibles passent par `translate` pipe ou service

### Performance
- [ ] `OnPush` sur tous les composants
- [ ] Pas de calcul dans les templates (tout dans `computed()`)
- [ ] Lazy loading sur toutes les routes
- [ ] Polling : `DestroyRef` pour les cleanup

---

## Rappels rapides

| Situation | Solution |
|---|---|
| Nouvelle feature | Copier la structure `api/components/data/pages/state/utils/routes` |
| Nouveau rôle | Ajouter dans `ROLES` (role.model.ts) + mettre à jour les guards + nav.config |
| Nouveau endpoint | Créer/compléter le `*.api.ts` de la feature |
| Nouvelle entité | Créer `*.model.ts` dans `shared/models/` si partagée, sinon dans la feature |
| Backend non prêt | `useStaticData: true` dans `environment.development.ts` + créer `mock-*.ts` |
| Export CSV | Utiliser `toCsv<T>()` + `downloadCsv()` dans `shared/utils/csv-export.ts` |
| Afficher selon rôle | `*appHasRole="ROLES.ADMINISTRATOR"` dans le template |
| Requête sans loader | Ajouter header `X-Skip-Loading: true` |
