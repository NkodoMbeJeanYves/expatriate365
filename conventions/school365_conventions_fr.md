# SCHOOL365 — Conventions et Décisions du Projet (FR)

**Projet :** School365  
**Type :** Plateforme de gestion scolaire, académique et financière  
**Version conventions :** 1.0 consolidée  
**Date :** 2026-07-02

---

## Table des matières

1. Conventions techniques définitives
2. Convention base de données et fournisseurs
3. Principes de conception
4. Convention de nommage
5. Architecture technique cible
6. Règles de gestion métier
7. Sécurité et RBAC
8. Contrat API REST
9. Recommandations de développement

---

## 1. Conventions techniques définitives

- Tout le code source doit être rédigé **exclusivement en anglais**.
- Backend : conventions .NET — PascalCase, interfaces préfixées par `I`.
- Frontend : conventions Angular — fichiers en kebab-case.
- API JSON : snake_case dans tous les payloads.
- Base de données : tables et colonnes en snake_case.
- DTOs : pattern de nommage `Request` / `Response`.
- Endpoints REST : ressources au pluriel.
- Routes API versionnées avec le préfixe `/api/v1`.
- Tous les endpoints sécurisés par JWT Bearer.
- Documentation des endpoints via Swagger / OpenAPI.

---

## 2. Convention base de données et fournisseurs (DB-CONF)

- SQLite et MySQL sont tous deux supportés.
- **SQLite est le fournisseur par défaut.**
- Le fournisseur est sélectionné via le fichier de configuration — aucun changement de code métier n'est nécessaire pour passer de SQLite à MySQL.
- Les couches **Domain** et **Application** ne doivent pas dépendre du moteur de base de données.
- Les migrations EF Core doivent rester compatibles SQLite et MySQL.
- Les paramètres de connexion doivent être externalisés.
- Le code spécifique au fournisseur doit être isolé dans la couche **Infrastructure**.
- SQLite et MySQL peuvent coexister dans la solution, mais seul le fournisseur configuré est activé à l'exécution.
- Le projet doit être livré avec une configuration SQLite fonctionnelle par défaut.

---

## 3. Principes de conception

| Code | Intitulé | Description |
|------|----------|-------------|
| PC-001 | Simplicité MVP | Le MVP doit privilégier les fonctionnalités essentielles et éviter la complexité inutile. |
| PC-002 | Modularité | Chaque domaine doit être isolé afin de permettre une évolution progressive. |
| PC-003 | Multi-établissement préparé | Même si le MVP commence avec un nombre limité d'établissements, les tables doivent contenir une référence à l'établissement lorsque cela est pertinent. |
| PC-004 | Données auditables | Les entités principales doivent contenir : `created_at`, `updated_at`, `created_by`, `updated_by`, `is_active`. |
| PC-005 | Génération automatique des périodes | Les périodes de cours ne doivent jamais être créées manuellement. Elles sont générées à partir du modèle de journée académique. |
| PC-006 | Préparation SQLite vers MySQL | Les types, contraintes et noms de colonnes doivent rester compatibles avec une future migration MySQL. |

---

## 4. Convention de nommage

### 4.1 Backend C#

- Classes, méthodes, propriétés : **PascalCase**
- Interfaces : préfixe `I` + PascalCase

```csharp
public class Student
{
    public Guid Id { get; set; }
    public string RegistrationNumber { get; set; }
}

public interface IStudentRepository { }
```

### 4.2 Base de données

- Tables : snake_case, **au pluriel**
- Colonnes : snake_case
- Clés primaires : `id` (UUID stocké en TEXT)
- Clés étrangères : `<entite>_id`

```sql
-- Exemples de noms valides
students
academic_years
school_id
created_at
```

### 4.3 API JSON

Toutes les réponses et requêtes API utilisent le **snake_case**.

```json
{
  "student_id": "uuid",
  "registration_number": "STD-2026-0001",
  "first_name": "Jean",
  "last_name": "Nkodo"
}
```

### 4.4 Frontend Angular

- Fichiers de composants, services, modules : **kebab-case**
- Interfaces TypeScript : alignées sur les DTO API
- Appels HTTP centralisés dans des services dédiés

---

## 5. Architecture technique cible

### 5.1 Frontend (Angular)

Technologies :
- Angular 20+, Angular Router, Angular Reactive Forms
- Angular Material ou équivalent
- Signals API pour la gestion d'état locale
- Services HTTP typés
- Guards d'authentification par rôle
- Intercepteur HTTP pour le token JWT
- Architecture modulaire par domaine

```text
frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   ├── shared/
│   │   ├── features/
│   │   │   ├── administration/
│   │   │   ├── academic/
│   │   │   ├── students/
│   │   │   ├── teachers/
│   │   │   ├── attendance/
│   │   │   ├── grades/
│   │   │   ├── billing/
│   │   │   └── dashboards/
│   │   └── layouts/
│   └── environments/
```

### 5.2 Backend (ASP.NET Core)

Technologies :
- ASP.NET Core 9 (ou version stable disponible)
- Entity Framework Core
- SQLite (MVP) / MySQL (évolution)
- JWT Bearer Authentication
- Clean Architecture (Domain / Application / Infrastructure / Api)

```text
backend/
├── School365.Api/
├── School365.Application/
├── School365.Domain/
├── School365.Infrastructure/
└── School365.Tests/
```

---

## 6. Règles de gestion métier

### 6.1 Gestion des établissements

| Code | Règle |
|------|-------|
| RG-A1-001 | Le nom de l'établissement est obligatoire. |
| RG-A1-002 | Le code de l'établissement doit être unique. |
| RG-A1-003 | Un établissement désactivé ne doit plus permettre la création de nouvelles inscriptions. |
| RG-A1-004 | Un établissement doit obligatoirement posséder un modèle de journée académique avant la création d'un emploi du temps. |
| RG-A1-005 | Un établissement peut être créé en répliquant la configuration d'un autre établissement (mode template/copie). |

### 6.2 Modèle de journée académique

| Code | Règle |
|------|-------|
| RG-JA-001 | L'heure de début de journée doit être inférieure à l'heure de fin de journée. |
| RG-JA-002 | La durée d'une période doit être strictement supérieure à 0. |
| RG-JA-003 | Une pause doit être comprise dans la journée académique. |
| RG-JA-004 | Deux pauses ne peuvent pas se chevaucher. |
| RG-JA-005 | Une période ne peut pas chevaucher une pause. |
| RG-JA-006 | Les périodes générées sont en lecture seule. |
| RG-JA-007 | Toute modification du modèle de journée académique doit déclencher une régénération des périodes. |
| RG-JA-008 | Si des emplois du temps existent déjà et sont utilisés par l'établissement, la modification doit être bloquée ou nécessiter une réinitialisation explicite. |
| RG-JA-009 | La somme des durées de périodes générées et des pauses doit être cohérente avec la plage horaire définie (heure fin − heure début). |
| RG-JA-010 | Lors de la configuration du premier jour, le système peut proposer d'appliquer la même configuration aux autres jours de la semaine. |

**Exemple fonctionnel :**

```text
Début journée : 08:00  |  Fin journée : 16:50  |  Durée période : 50 min
Pause 1 : 10:30 (15 min)  |  Pause 2 : 12:25 (35 min)  |  Pause 3 : 15:30 (10 min)

P1 : 08:00 - 08:50    P2 : 08:50 - 09:40    P3 : 09:40 - 10:30
Pause 1 : 10:30 - 10:45
P4 : 10:45 - 11:35    P5 : 11:35 - 12:25
Pause 2 : 12:25 - 13:00
P6 : 13:00 - 13:50    P7 : 13:50 - 14:40    P8 : 14:40 - 15:30
Pause 3 : 15:30 - 15:40
P9 : 15:40 - 16:30

La plage 16:30 - 16:50 ne génère pas de période (inférieure à 50 min).
```

### 6.3 Années académiques

| Code | Règle |
|------|-------|
| RG-A2-001 | Une seule année académique peut être active pour un établissement. |
| RG-A2-002 | La date de début doit être inférieure à la date de fin. |
| RG-A2-003 | Une année académique clôturée ne peut plus recevoir de nouvelles notes ou absences. |

### 6.4 Référentiel académique (cycles, niveaux, classes, matières)

| Code | Règle |
|------|-------|
| RG-B1-001 | Un cycle appartient à un établissement. |
| RG-B1-002 | Un cycle peut contenir plusieurs niveaux. |
| RG-B4-001 | Une classe appartient à une année académique. |
| RG-B4-002 | Une classe appartient à un établissement. |
| RG-B4-003 | Un apprenant ne peut être inscrit que dans une seule classe active pour une année académique donnée. |
| RG-B4-004 | La capacité maximale ne doit pas être dépassée sauf autorisation explicite d'un administrateur. |
| RG-B5-001 | Le code matière doit être unique dans un établissement. |
| RG-B5-002 | Le coefficient doit être supérieur ou égal à 0. |
| RG-B5-003 | Le nombre de périodes par séance doit être supérieur à 0. |

### 6.5 Inscriptions

| Code | Règle |
|------|-------|
| RG-C1-001 | Le matricule d'un apprenant doit être unique. |
| RG-C1-002 | Un apprenant doit être rattaché à un établissement. |
| RG-C2-001 | Un apprenant doit avoir au moins un contact responsable (contexte primaire et secondaire). |
| RG-C3-001 | Un apprenant ne peut pas avoir deux inscriptions validées dans deux classes différentes pour la même année académique. |
| RG-C3-002 | Une inscription validée doit créer ou mettre à jour le dossier scolaire de l'apprenant. |
| RG-C3-003 | Une inscription validée peut déclencher la génération automatique de frais d'inscription. |

### 6.6 Emplois du temps

| Code | Règle |
|------|-------|
| RG-E1-001 | Un cours doit utiliser uniquement des périodes générées. |
| RG-E1-002 | Un cours peut occuper plusieurs périodes consécutives. |
| RG-E1-003 | Un enseignant ne peut pas être affecté à deux cours au même moment. |
| RG-E1-004 | Une classe ne peut pas avoir deux cours au même moment. |
| RG-E1-005 | Une salle ne peut pas être utilisée par deux cours au même moment. |
| RG-E1-006 | Un cours ne peut pas traverser une pause. |
| RG-E1-007 | Si un cours nécessite 2 périodes, le système doit vérifier que les 2 périodes sont consécutives et disponibles. |

### 6.7 Absences et présences

| Code | Règle |
|------|-------|
| RG-F1-001 | Un appel est lié à une date, une classe, une matière et éventuellement un cours planifié. |
| RG-F1-002 | Un apprenant ne peut avoir qu'un seul statut de présence pour le même cours. |
| RG-F1-003 | Un retard peut être associé à une durée en minutes. |
| RG-F1-004 | Une absence excusée doit contenir un motif ou un justificatif. |

Statuts possibles : **présent**, **absent**, **retard**, **excusé**.

### 6.8 Notes et évaluations

| Code | Règle |
|------|-------|
| RG-G2-001 | Une évaluation doit être rattachée à une classe et une matière. |
| RG-G2-002 | La note maximale doit être supérieure à 0. |
| RG-G2-003 | Le coefficient doit être supérieur à 0. |
| RG-G3-001 | Une note ne peut pas être supérieure à la note maximale de l'évaluation. |
| RG-G3-002 | Une note ne peut pas être négative. |
| RG-G3-003 | Une note verrouillée ne peut être modifiée que par un profil autorisé. |
| RG-G4-001 | Le calcul doit tenir compte des coefficients des évaluations. |
| RG-G4-002 | Le calcul général doit tenir compte des coefficients des matières. |
| RG-G4-003 | Les absences aux évaluations doivent être traitées selon une règle configurable : ignorée / note zéro / évaluation à reprendre. |

**Formules de calcul :**

```text
moyenne_matiere  = somme(note_normalisee × coefficient_evaluation) / somme(coefficients_evaluation)
moyenne_generale = somme(moyenne_matiere × coefficient_matiere)    / somme(coefficients_matieres)
```

### 6.9 Bulletins

| Code | Règle |
|------|-------|
| RG-H1-001 | Un bulletin ne peut être généré que si les notes nécessaires sont disponibles. |
| RG-H1-002 | Un bulletin validé doit être archivé. |
| RG-H1-003 | Un bulletin validé ne peut être modifié sans réouverture par un profil autorisé. |

### 6.10 Paiements et facturation

| Code | Règle |
|------|-------|
| RG-I1-001 | Un type de frais appartient à un établissement. |
| RG-I1-002 | Un type de frais peut être obligatoire ou optionnel. |
| RG-I2-001 | Une facture doit être liée à un apprenant. |
| RG-I2-002 | Une facture émise doit posséder un numéro unique. |
| RG-I2-003 | Le total facture est égal à la somme des lignes moins les remises. |
| RG-I2-004 | Une facture payée ne peut pas être supprimée. |
| RG-I3-001 | Un paiement doit avoir un montant strictement supérieur à 0. |
| RG-I3-002 | Un paiement ne peut pas dépasser le solde restant de la facture sauf si le système autorise les avances. |
| RG-I3-003 | Tout paiement validé doit générer un reçu. |
| RG-I3-004 | Un reçu doit avoir un numéro unique. |

**Modes de paiement supportés :** espèces, cash, virement, carte, mobile money, chèque.

---

## 7. Sécurité et RBAC

### 7.1 Authentification

- JWT Bearer Authentication obligatoire sur tous les endpoints protégés.

### 7.2 Rôles initiaux

```text
super_admin   school_admin   director   registrar
teacher       accountant     parent     student
```

### 7.3 Règles RBAC

| Code | Règle |
|------|-------|
| RG-RBAC-001 | Un utilisateur ne doit accéder qu'aux données de son établissement, sauf super administrateur. |
| RG-RBAC-002 | Un enseignant ne peut saisir des notes que pour ses classes et matières affectées. |
| RG-RBAC-003 | Un parent ne peut consulter que les données des apprenants qui lui sont associés. |
| RG-RBAC-004 | Un comptable peut gérer les factures et paiements, mais ne peut pas modifier les notes. |

### 7.4 Permissions initiales

```text
schools.read          schools.create        schools.update
academic_years.manage students.manage       teachers.manage
attendance.manage     grades.manage         invoices.manage
payments.manage       reports.read
```

---

## 8. Contrat API REST

### 8.1 Principes généraux

- REST + JSON, snake_case dans tous les payloads
- Routes versionnées : `/api/v1`
- Sécurisation JWT, documentation OpenAPI / Swagger

### 8.2 Endpoints

#### Administration

```text
GET    /api/v1/schools
POST   /api/v1/schools
GET    /api/v1/schools/{school_id}
PUT    /api/v1/schools/{school_id}
DELETE /api/v1/schools/{school_id}
```

#### Modèle de journée académique

```text
GET  /api/v1/schools/{school_id}/academic-day-template
POST /api/v1/schools/{school_id}/academic-day-template
PUT  /api/v1/academic-day-templates/{template_id}
POST /api/v1/academic-day-templates/{template_id}/generate-periods
GET  /api/v1/academic-day-templates/{template_id}/generated-periods
```

#### Années académiques

```text
GET  /api/v1/schools/{school_id}/academic-years
POST /api/v1/schools/{school_id}/academic-years
PUT  /api/v1/academic-years/{academic_year_id}
POST /api/v1/academic-years/{academic_year_id}/activate
POST /api/v1/academic-years/{academic_year_id}/close
```

#### Apprenants

```text
GET  /api/v1/students
POST /api/v1/students
GET  /api/v1/students/{student_id}
PUT  /api/v1/students/{student_id}
```

#### Inscriptions

```text
GET  /api/v1/enrollments
POST /api/v1/enrollments
GET  /api/v1/enrollments/{enrollment_id}
POST /api/v1/enrollments/{enrollment_id}/validate
POST /api/v1/enrollments/{enrollment_id}/cancel
```

#### Absences

```text
GET  /api/v1/attendance-sessions
POST /api/v1/attendance-sessions
GET  /api/v1/attendance-sessions/{attendance_session_id}
POST /api/v1/attendance-sessions/{attendance_session_id}/records
PUT  /api/v1/attendance-records/{attendance_record_id}
```

#### Notes

```text
GET  /api/v1/evaluations
POST /api/v1/evaluations
GET  /api/v1/evaluations/{evaluation_id}
POST /api/v1/evaluations/{evaluation_id}/grades
PUT  /api/v1/grades/{grade_id}
POST /api/v1/evaluations/{evaluation_id}/lock
```

#### Paiements

```text
GET  /api/v1/invoices
POST /api/v1/invoices
GET  /api/v1/invoices/{invoice_id}
POST /api/v1/invoices/{invoice_id}/payments
GET  /api/v1/payments/{payment_id}/receipt
```

---

## 9. Recommandations de développement

### 9.1 Angular

- Créer un module par domaine fonctionnel.
- Utiliser des interfaces TypeScript alignées sur les DTO API.
- Centraliser les appels HTTP dans des services.
- Créer des composants smart/dumb lorsque nécessaire.
- Utiliser Reactive Forms pour les formulaires complexes.
- Utiliser des guards par rôle.
- Utiliser un intercepteur pour injecter le token JWT.
- Prévoir un design responsive.

### 9.2 ASP.NET Core

- Séparer Domain, Application, Infrastructure et Api.
- Utiliser des DTO pour les contrats API — ne jamais exposer directement les entités EF.
- Utiliser FluentValidation ou équivalent.
- Utiliser les migrations EF Core.
- Centraliser la gestion des erreurs.
- Utiliser des services applicatifs par domaine.
- Documenter tous les endpoints via Swagger.
- Écrire des tests unitaires pour les règles critiques : génération des périodes, calcul des moyennes, calcul des soldes.

---

*Fin du document — school365_conventions_fr.md*
