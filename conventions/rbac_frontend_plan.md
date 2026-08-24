# RBAC Frontend Plan — School365

> Document de référence pour l'implémentation du contrôle d'accès basé sur les rôles (RBAC) dans le frontend Angular. Généré le 2026-07-12.

---

## 1. Rôles définis (backend → frontend)

8 rôles snake_case, définis dans le seeder backend (`School365.Infrastructure/Persistence/DbSeeder.cs`) et utilisés tels quels en Angular :

| Rôle | Description |
|---|---|
| `super_admin` | Administrateur plateforme — accès total, pas de `school_id` |
| `school_admin` | Administrateur d'établissement |
| `director` | Directeur |
| `registrar` | Secrétaire / Scolarité |
| `teacher` | Enseignant |
| `accountant` | Comptable |
| `parent` | Parent / tuteur |
| `student` | Élève / apprenant |

> Il n'existe pas de fichier de constantes dédié aux rôles côté Angular — ils sont des strings inline. Un fichier `core/auth/models/role.model.ts` avec `as const` est recommandé pour éviter les typos.

---

## 2. Permissions par rôle (backend)

Permissions définies dans le seeder (`DbSeeder.cs`) et renvoyées via `GET /v1/auth/me` dans `MeResponse.permissions` :

| Permission | super_admin | school_admin | director | registrar | teacher | accountant | parent | student |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `schools.read` | ✓ | ✓ | ✓ | ✓ | | | | |
| `schools.create` | ✓ | | | | | | | |
| `schools.update` | ✓ | ✓ | | | | | | |
| `schools.manage` | ✓ | | | | | | | |
| `academic.manage` | ✓ | ✓ | | ✓ | | | | |
| `students.manage` | ✓ | ✓ | ✓ | ✓ | | | | |
| `teachers.manage` | ✓ | ✓ | ✓ | | | | | |
| `attendance.manage` | ✓ | ✓ | ✓ | ✓ | ✓ | | | |
| `grades.manage` | ✓ | ✓ | | | | | | |
| `grades.enter` | ✓ | | | | ✓ | | | |
| `grades.read` | ✓ | | | | ✓ | | ✓ | ✓ |
| `invoices.manage` | ✓ | ✓ | | | | ✓ | | |
| `invoices.read` | ✓ | | ✓ | | | ✓ | ✓ | |
| `payments.manage` | ✓ | ✓ | | | | ✓ | | |
| `reports.read` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `reports.validate` | ✓ | ✓ | ✓ | | | | | |
| `users.audit` | ✓ | ✓ | ✓ | | | | | |

---

## 3. Guards Angular existants

### `authGuard` — `core/guards/auth.guard.ts`
Vérifie la présence du token en localStorage via `AuthService.isAuthenticated()`. Redirige vers `/login` si absent.

### `roleGuard` — `core/guards/role.guard.ts`
Lit `route.data['roles']` et appelle `CurrentUserService.hasAnyRole(roles)`. Redirige vers `/unauthorized` si refus. Si `data.roles` est vide, laisse passer.

### `yearSelectionGuard` — `core/guards/year-selection.guard.ts`
Vérifie qu'une année scolaire est active en session. Les `super_admin` (pas de `school_id`) sont exemptés. Ouvre le modal de sélection d'année si aucune n'est active.

### `emailVerificationGuard` — `core/guards/email-verification.guard.ts`
Défini mais non utilisé dans les routes actuelles.

---

## 4. Protection des routes (`app.routes.ts`)

### Portails racine

| Chemin | Guards | Rôles requis |
|---|---|---|
| `/admin` | `authGuard, roleGuard, yearSelectionGuard` | `super_admin`, `school_admin`, `director`, `registrar`, `accountant` |
| `/teacher` | `authGuard, roleGuard, yearSelectionGuard` | `teacher` |
| `/parent` | `authGuard, roleGuard, yearSelectionGuard` | `parent` |
| `/student` | `authGuard, roleGuard, yearSelectionGuard` | `student` |

### Routes enfants avec restriction supplémentaire

| Chemin (sous `/admin`) | Guards | Rôles requis |
|---|---|---|
| `users` | `roleGuard` | `super_admin`, `school_admin` |
| `audit` | `roleGuard` | `super_admin`, `school_admin` |
| `users/:id/audit` | `roleGuard` | `super_admin`, `school_admin` |

---

## 5. Service central — `CurrentUserService`

**Fichier :** `core/services/current-user.service.ts`

```typescript
currentUser = signal<MeResponse | null>(null);

hasRole(role: string): boolean          // vérifie un rôle exact
hasAnyRole(roles: string[]): boolean    // vérifie au moins un rôle parmi la liste
isEmailVerified(): boolean
```

- L'état utilisateur est un `signal<MeResponse | null>`.
- `MeResponse.permissions: string[]` est reçu du backend via `/v1/auth/me` mais **n'est pas encore consommé** côté frontend — pas de `hasPermission()`.
- Pas de décodage JWT — les rôles/permissions sont chargés exclusivement via l'API `/me`.

---

## 6. Modèles TypeScript (`core/models/auth.model.ts`)

```typescript
interface UserInfo {
  id: string; email: string; full_name: string;
  roles: string[];
  school_id?: string;          // absent pour super_admin
  entity_type?: string;
  entity_id?: string;
  email_verified_at?: string;
}

interface MeResponse extends UserInfo {
  permissions: string[];       // reçu mais non utilisé dans les composants
}
```

---

## 7. État actuel — lacunes identifiées

| Lacune | Impact | Priorité |
|---|---|---|
| Pas de constantes de rôles (`role.model.ts`) | Typos silencieux possibles | MOYENNE |
| `permissions` non utilisées côté frontend | Contrôle fin impossible sans refactoring | BASSE |
| Nav admin non filtrée par rôle | `director`, `registrar`, `accountant` voient tous les liens | MOYENNE |
| Pas de directive `[appHasRole]` | Les checks de visibilité inline sont verbeux | BASSE |
| `emailVerificationGuard` non branché | La protection email n'est pas appliquée | BASSE |

---

## 8. Pattern recommandé — vérification de rôle en composant

```typescript
// Injection
private readonly currentUserService = inject(CurrentUserService);

// Méthode booléenne
canEdit(): boolean {
  return this.currentUserService.hasAnyRole(['super_admin', 'school_admin']);
}

// Dans le template
@if (canEdit()) { <button>Modifier</button> }
```

---

## 9. Pattern recommandé — filtrage nav par rôle (à implémenter)

Dans `admin-layout.component.ts`, étendre `NavItem` avec un champ `roles?: string[]` optionnel, puis calculer `visibleNavItems` avec un `computed()` :

```typescript
interface NavItem {
  icon: string; label: string; route: string;
  separator?: boolean;
  roles?: string[];   // si absent : visible par tous les rôles admin
}

visibleNavItems = computed(() =>
  this.navItems.filter(item =>
    !item.roles || this.currentUserService.hasAnyRole(item.roles)
  )
);
```

---

## 10. Règles RBAC métier clés (RG-RBAC)

| Code | Règle | Implémentation actuelle |
|---|---|---|
| RG-RBAC-001 | Un utilisateur n'accède qu'aux données de son école | Via `school_id` dans les appels API |
| RG-RBAC-002 | Un enseignant ne saisit les notes que pour ses classes/matières | Filtrage API côté backend uniquement |
| RG-RBAC-003 | Un parent ne voit que les données de ses élèves | Portail `/parent` séparé, filtrage backend |
| RG-RBAC-004 | Un comptable gère factures/paiements mais pas les notes | Portail `/admin` avec permissions backend |
