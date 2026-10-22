# Plan d’action — Assistance communautaire au transfert de fonds

**Nom du module :** Community Transfer Assistance  
**Nom français :** Assistance communautaire au transfert de fonds  
**Statut :** Lots 1 à 6 et première tranche de préparation à la production (PROD-0 à PROD-2) approuvés. Correctifs de cette tranche implémentés ; exécution MySQL en CI et décisions opérationnelles encore attendues. Aucune activation en production.
**Mise à jour :** 22 octobre 2026.
**Base de données cible :** MySQL uniquement. SQLite est retiré du périmètre sur décision produit du 22 octobre 2026 ; aucune implémentation ni validation SQLite n’est prévue.

### MFA optionnelle pour le MVP non transactionnel

- `TransferAssistance:RequireMfa` est explicitement configuré à `false` pour le MVP. Si le paramètre est absent, sa valeur par défaut dans le code est `true`.
- La même option gouverne l'inscription volontaire, la consultation des demandes disponibles, les propositions, la recherche et l'invitation de témoins, leur acceptation et la déclaration de référence externe.
- Les autres contrôles restent inchangés : comptes et membres actifs, contrôles existants d'e-mail vérifié, volontariat, vérification manuelle distincte, permissions, isolation et confirmations de remise.
- Aucun champ MFA utilisateur n'est modifié. La MFA complète (activation et second facteur à la connexion) n'est pas encore implémentée ; la vérification manuelle ne protège pas contre le vol de compte.
- Pour réactiver l'exigence, configurer `TransferAssistance:RequireMfa` à `true` (ou `TransferAssistance__RequireMfa=true` dans l'environnement) puis redémarrer l'API. Ne pas activer artificiellement `MfaEnabled` : livrer d'abord un véritable parcours MFA.
- Cette option ne concerne que l'assistance au transfert et ne rend pas le MVP transactionnel.

### Stabilisation de la session et du formulaire

- Les confirmations du workflow utilisent le dialogue PrimeNG commun (annulation, sélection d'aidant, invitation de témoin, remise, attestation, référence externe, réception et litiges), sans fenêtre native du navigateur.

- La connexion, le renouvellement et `/auth/me` partagent la résolution du membre actif dans le tenant ; `/auth/me` utilise les permissions effectives, sans attribuer de nouveaux droits.
- Un membre du bureau reste un membre pour le formulaire, sous réserve des permissions requises. Un compte sans membre lié ne peut pas créer de demande.
- La page recalcule ses conditions lors des changements de session, recharge les données utiles et annule les abonnements de l'ancien contexte. Les brouillons locaux, recherches et données précédentes sont effacés au changement de compte ou de tenant.
- Des messages FR/EN distinguent le chargement de la session, l'absence de membre lié et l'absence de permission de création.

## 1. Objectif et limites

### Accréditations aidant et témoin

- `org_admin` ne peut pas demander ni obtenir ces accréditations, ni être choisi pour une nouvelle participation aidant/témoin, même si son profil a été vérifié avant ce changement. Ses droits de revue et ses usages demandeur/bénéficiaire sont conservés.
- Une demande non annulée par membre, tenant et rôle de participation : les états en attente, vérifié et rejeté bloquent une nouvelle demande. L'annulation ou le retrait explicite est requis avant une nouvelle vérification.
- Le client propose des actions séparées par rôle, avec confirmation PrimeNG pour l'annulation/retrait. Le serveur conserve la compatibilité des anciens payloads booléens, sans créer d'événement supplémentaire pour un enregistrement identique.
- L'annulation remet le rôle à `not_requested`, conserve son historique dans l'audit et ne modifie aucune mission déjà engagée.
- Les champs existants de volontariat et de statut servent de jetons de concurrence EF ; l'index unique tenant/membre protège la création simultanée. Aucun changement physique du schéma n'est requis.

Permettre à des membres de demander, proposer et suivre une assistance communautaire liée à un transfert familial, avec vérification des participants, témoin, preuves et audit.

Le MVP est **non transactionnel** : l’application organise le parcours et enregistre son suivi, mais ne reçoit, ne conserve, ne convertit ni ne transfère de fonds. Un transfert réel ne pourra être envisagé qu’après validation juridique et opérationnelle et via un canal ou un prestataire autorisé. Un témoin apporte une preuve de ce qu’il a observé ; il ne remplace ni les contrôles de conformité ni le fournisseur de transfert.

## 2. État des lieux et décisions à confirmer

- Le serveur est structuré en `API`, `Application`, `Domain` et `Infrastructure`, avec des endpoints Minimal API, MediatR, EF Core et MySQL via Pomelo. Le nouveau domaine doit suivre cette organisation, sans placer de logique métier dans les endpoints.
- Le client utilise des fonctionnalités par domaine, des services HTTP, des modèles TypeScript et deux catalogues de traduction (`en.json`, `fr.json`).
- Les versions actuellement déclarées diffèrent de certaines conventions de référence : le client déclare Angular 22, PrimeNG 22 et Tailwind 3.4 ; le serveur cible .NET 10 avec des packages EF Core 9. Avant le développement, confirmer la matrice de versions prise en charge et éviter une mise à niveau ou un ajout de dépendance uniquement pour ce module.
- Les routes des fonctionnalités existantes utilisent des fichiers de routes lazy-loadés. La convention cible demande des composants standalone chargés par `loadComponent`. Décider de la règle à suivre pour le nouveau module en cohérence avec l’application et résoudre l’écart documentaire sans refactoriser les fonctionnalités hors périmètre.
- Confirmer le cloisonnement attendu : tenant/association, plateforme entière, ou un modèle mixte pour les opérations de conformité. Le serveur utilise aujourd’hui le claim `tenant_id`.
- Confirmer, avant d’implémenter les contrôles, les exigences de vérification d’identité, les corridors, les durées de rétention, les motifs de conservation des preuves et les conditions de recours aux prestataires.

### Décisions de cadrage recueillies

- **Périmètre :** opérations cloisonnées par tenant/association ; pas de visibilité inter-tenant.
- **MVP :** aucun mouvement réel de fonds ; suivi communautaire non transactionnel uniquement.
- **Revue conformité :** rôles `org_admin` et `auditor` dans le tenant concerné. Les actions devront exiger des permissions dédiées, vérifiées côté serveur ; cette décision ne donne pas un accès global ni une permission implicite à d’autres rôles.
- **Participants :** un membre actif peut demander une assistance ; aidants et témoins doivent être vérifiés et s’inscrire volontairement. Les critères et preuves de vérification restent à spécifier avant la migration.
- **Vérification MVP :** attestation manuelle par `org_admin`/`auditor` ; aucun document d’identité n’est téléversé ou conservé dans le module.
- **Confirmation bénéficiaire :** seul un membre actif du même tenant, disposant d’un compte actif et d’une adresse e-mail vérifiée, peut être sélectionné et confirmer après authentification. Aucun contact ou compte financier externe n’est collecté.

## 3. Périmètre du MVP

### Inclus

1. Profil d’éligibilité du demandeur, de l’aidant et du témoin ; activation volontaire du rôle d’aidant.
2. Déclaration et gestion d’un bénéficiaire, avec exposition limitée de ses coordonnées.
3. Création, soumission, revue, approbation ou rejet d’une demande.
4. Publication anonymisée aux aidants éligibles et dépôt d’une proposition.
5. Sélection d’un aidant et invitation/acceptation d’un témoin indépendant.
6. Planification d’une remise, confirmations des participants et reçu de suivi.
7. Enregistrement d’une preuve de transfert communiqué par le fournisseur, sans exécuter le transfert.
8. Confirmation par le bénéficiaire, traitement de réception totale/partielle/non reçue et clôture conditionnelle.
9. File de revue conformité, suspension, signalement et piste d’audit.
10. Notifications des changements importants et interfaces adaptées aux rôles.

### Hors périmètre initial

- Conservation ou mouvement de fonds par Expatriate.
- Échange de devises, fixation de taux ou commission informelle entre membres.
- Paiement en plusieurs fractions ou partage d’une demande entre plusieurs aidants.
- Automatisation KYC/AML ou décision automatisée de conformité.
- Intégration d’un prestataire financier avant validation juridique, commerciale et technique.
- Promesse de remboursement par Expatriate, l’aidant ou le témoin.

## 4. Phases et ordre d’exécution

Les phases sont séquentielles lorsque la suivante dépend d’une décision ou d’un livrable de la précédente.

### Phase 0 — Cadrage juridique, opérationnel et produit

- [ ] Documenter qui remet, reçoit et transfère les fonds dans le modèle cible ; qui fixe le taux et les frais ; qui supporte une erreur, un retard, un échec ou un remboursement.
- [ ] Présenter le modèle envisagé à un conseiller juridique spécialisé à Maurice, aux banques concernées et aux prestataires de transfert envisagés.
- [ ] Choisir le périmètre du pilote : tenant/plateforme, corridor, devise, canal autorisé, critères d’éligibilité et plafonds produit.
- [ ] Définir les règles de suspension, d’annulation avant/après remise, de litige et de conservation des éléments.
- [ ] Définir les niveaux de vérification et les moyens de preuve acceptables. Ne pas présenter les plafonds du produit comme des seuils réglementaires.
- [ ] Décider si un compte bancaire/mobile-money vérifié et la MFA sont prérequis du rôle aidant ; spécifier les OTP, leur durée, leur usage unique et la limitation des tentatives.
- **Sortie / porte de décision :** modèle opérationnel approuvé pour un prototype sans transfert réel ; aucune opération réelle avant validation formelle du canal et du cadre applicable.

### Phase 1 — Conception fonctionnelle et contrats

- [ ] Modéliser le workflow et les transitions autorisées (section 5).
- [ ] Définir les rôles, permissions, politiques de visibilité et les responsabilités des opérateurs.
- [ ] Finaliser le schéma logique, le contrat API snake_case, les règles de pagination et les formats d’erreur du projet.
- [ ] Définir les champs sensibles, leur chiffrement, leur masquage, les journaux d’accès et la durée de conservation.
- [ ] Définir les événements auditables et le modèle d’idempotence/concurrence des commandes critiques.
- [ ] Définir les états d’interface, les contenus anonymisés, les traductions et les notifications.
- **Sortie :** spécifications API/données et critères d’acceptation validés avant la migration de base de données.

### Phase 2 — Backend MVP

#### 2.1 Modèle de domaine et persistance

Créer le module dans les emplacements existants, en respectant les conventions C#, EF Core et les noms de tables/colonnes snake_case :

- `server/Domain/Entities/` : entités de demande, bénéficiaire, offre/affectation, invitation de témoin, session de remise, exécution déclarée, preuve, confirmation bénéficiaire, revue/alerte de risque.
- `server/Infrastructure/Persistence/Configurations/` : configurations EF explicites, clés, index, longueurs, contraintes et conversions compatibles avec la version MySQL de production.
- `server/Infrastructure/Persistence/AppDbContext.cs` : DbSet et configuration du nouveau domaine.
- Migration EF Core vérifiée sur le provider par défaut et contrôlée pour compatibilité avec le second provider.

Modèle conceptuel initial à valider : `transfer_requests`, `transfer_beneficiaries`, `transfer_offers`, `transfer_assignments`, `witness_assignments`, `handover_sessions`, `transfer_executions`, `transfer_evidence`, `beneficiary_confirmations`, `risk_flags`, `compliance_reviews`. Les litiges et remboursements peuvent être ajoutés au MVP seulement si les règles et responsabilités ont été définies en phase 0 ; sinon, les exposer comme périmètre ultérieur.

Chaque enregistrement opérationnel doit être rattaché au bon périmètre d’accès et à ses acteurs. Conserver une copie immuable des données nécessaires au reçu et à l’audit, sans dupliquer inutilement les pièces sensibles. Ne jamais supprimer physiquement une opération financière déclarée.

#### 2.2 Application et règles métier

- [ ] Ajouter des DTO `Request`/`Response` et validateurs FluentValidation selon les modèles existants.
- [ ] Implémenter commandes/requêtes MediatR séparées par cas d’usage : création/modification de brouillon, soumission, revue, offre, sélection, affectation de témoin, confirmations de remise, ajout de preuve, déclaration de résultat bénéficiaire, suspension et résolution.
- [ ] Vérifier les règles dans la couche application/domaine, et pas uniquement dans les formulaires ou les endpoints.
- [ ] Vérifier côté serveur le statut actif/vérifié, les rôles, les limites configurées, le corridor disponible, l’absence de conflit d’intérêts du témoin et l’absence d’opération incompatible déjà ouverte.
- [ ] Interdire le changement de bénéficiaire après confirmation de la remise ; toute correction nécessaire doit suivre un parcours audité et une revue explicite.
- [ ] Rendre les commandes sensibles idempotentes et gérer les mises à jour concurrentes afin qu’une double requête ne confirme pas deux fois une opération.
- [ ] Journaliser les décisions et changements de statut avec identifiant d’acteur, date, motif et corrélation, sans écrire de secrets ou de coordonnées complètes dans les logs.

#### 2.3 API et autorisation

Créer `server/API/TransferAssistance/TransferAssistanceEndpoints.cs`, l’enregistrer dans `Program.cs` et ajouter les permissions dans `server/Application/Common/Permissions.cs` ainsi que dans le mécanisme de rôles/permissions existant.

Contrat de routes proposé, à confirmer en phase 1 :

| Méthode | Route | Usage |
|---|---|---|
| `GET` | `/api/v1/transfer-requests` | Demandes accessibles selon rôle, filtres et pagination |
| `POST` | `/api/v1/transfer-requests` | Créer un brouillon |
| `GET` | `/api/v1/transfer-requests/{id}` | Lire une demande autorisée, champs sensibles masqués |
| `PUT` | `/api/v1/transfer-requests/{id}` | Modifier un brouillon autorisé |
| `POST` | `/api/v1/transfer-requests/{id}/submit` | Soumettre à la revue |
| `POST` | `/api/v1/transfer-requests/{id}/offers` | Déposer une proposition d’aidant |
| `POST` | `/api/v1/transfer-requests/{id}/select-helper` | Sélectionner une proposition |
| `POST` | `/api/v1/transfer-requests/{id}/witness-invitations` | Inviter un témoin éligible |
| `POST` | `/api/v1/transfer-requests/{id}/handover/confirmations` | Ajouter une confirmation de remise selon l’acteur |
| `POST` | `/api/v1/transfer-requests/{id}/transfer-evidence` | Enregistrer une preuve fournie par le canal déclaré |
| `POST` | `/api/v1/transfer-requests/{id}/beneficiary-confirmation` | Enregistrer le résultat communiqué par le bénéficiaire |
| `POST` | `/api/v1/transfer-requests/{id}/cancel` | Annuler selon l’état et les règles |
| `GET` | `/api/v1/admin/transfer-reviews` | File de revue conformité autorisée |
| `POST` | `/api/v1/admin/transfer-requests/{id}/review` | Approuver, rejeter, demander des informations ou suspendre |

Tous les endpoints doivent appliquer JWT, permissions et contrôle d’accès sur la ressource, ses participants et son tenant. Une permission d’interface ne constitue pas un contrôle d’accès. Répondre avec l’enveloppe paginée standard du projet ; erreurs au format `{ "error": "..." }` et codes HTTP adaptés (`400`, `404`, `409`, `403`).

Permissions à préciser puis enregistrer dans le système existant :

```text
transfer_request.create
transfer_request.view_own
transfer_request.view_available
transfer_offer.submit
transfer_assignment.accept
transfer_beneficiary.manage_own
handover.confirm
witness.accept
witness.attest
transfer_evidence.upload
beneficiary.confirm
compliance.review
transaction.suspend
dispute.open
dispute.resolve
audit_log.view
```

Les rôles métier (`member`, `transfer_requester`, `transfer_helper`, `witness`, `support_agent`, `compliance_officer`, `finance_auditor`, `platform_administrator`) doivent être mappés explicitement aux rôles/permissions réellement supportés par l’application, sans accorder automatiquement des droits à tout membre ou administrateur de tenant.

#### 2.4 Pièces, audit et documentation

- [ ] Réutiliser le service de stockage/téléversement existant après vérification de ses contrôles d’accès ; servir les pièces via des URLs temporaires et autorisées.
- [ ] Ajouter validation de type/taille, détection de fichiers dangereux et empreinte de fichier ; ne pas exposer de documents d’identité dans les listes ou logs.
- [ ] Masquer les comptes, téléphones et références sensibles dans les API et les reçus non destinés à l’acteur concerné.
- [ ] Documenter les endpoints, permissions, erreurs et exemples JSON OpenAPI.
- [ ] Émettre des notifications pour les événements critiques via les mécanismes existants, sans y inclure de données financières complètes.

### Phase 3 — Frontend MVP

Créer une fonctionnalité `client/src/app/features/transfer-assistance/`, avec le modèle TypeScript, un service HTTP dédié, l’état local conforme aux patterns du projet, les routes, les composants standalone et les clés de traduction. Ajouter l’alias `@transfer-assistance/*` dans `client/tsconfig.json` si le module adopte cette frontière d’import.

#### Parcours demandeur

- [ ] Liste et suivi de ses demandes, avec statut et prochaine action.
- [ ] Formulaire de création/modification en brouillon : montant et devise de remise, destination, devise/canal souhaité, motif et relation au bénéficiaire, période et zone approximative.
- [ ] Gestion du bénéficiaire et saisie des données nécessaires, en expliquant leur usage et en masquant les coordonnées dans les vues récapitulatives.
- [ ] Récapitulatif des contrôles et consentements ; confirmation avant soumission.
- [ ] Consultation des offres, sélection d’un aidant, suivi de la session et confirmation de remise.
- [ ] Suivi du résultat de transfert et déclaration de réception totale, partielle ou non reçue ; ouverture d’un incident.

#### Parcours aidant et témoin

- [ ] File de demandes disponibles, ne présentant que destination, montant, canal, zone/période et informations minimales anonymisées.
- [ ] Détail anonymisé, soumission d’offre avec disponibilité et délai proposé ; frais/taux uniquement s’ils sont officiellement communiqués et validés.
- [ ] Vue de l’opération assignée avec étapes explicites, consignes de confirmation et dépôt de preuve.
- [ ] Invitations témoin, déclaration de conflit d’intérêts, consentement, vérification de présence et attestation limitée à ce que le témoin a observé.
- [ ] Code de session à usage limité selon le protocole backend ; ne jamais faire confiance à un code conservé ou validé uniquement côté client.

#### Back-office et UX transversale

- [ ] File de revue avec informations nécessaires, alertes explicables, justificatifs autorisés et actions auditées.
- [ ] Vues séparées pour suspensions, incidents et opérations en attente ; ne pas fournir d’action administrative irréversible sans confirmation.
- [ ] Écrans de chargement, erreurs, état vide, conflit/expiration et permissions insuffisantes.
- [ ] Ajouter les traductions françaises et anglaises en maintenant une parité stricte dans `client/public/i18n/fr.json` et `en.json`.
- [ ] Respecter l’accessibilité, l’usage mobile, le contraste, les libellés de formulaires et la confirmation des actions sensibles.
- [ ] Ne pas placer les coordonnées financières, justificatifs ou décisions de conformité dans le stockage local non protégé.

### Phase 4 — Qualité, sécurité et pilote sans transfert

- [ ] Tests unitaires des validateurs, règles d’éligibilité et transitions de statut.
- [ ] Tests d’intégration des handlers et persistance : transaction annulée, conflits, double soumission, preuve réutilisée et réception partielle.
- [ ] Tests API d’autorisation : demandeur, aidant, témoin et conformité ; accès inter-tenant interdit ; champs et pièces visibles seulement aux acteurs autorisés.
- [ ] Tests frontend des formulaires, masquage, rôles, transitions d’écrans et erreurs HTTP ; tests end-to-end du parcours non transactionnel.
- [ ] Vérifier migrations et contraintes sur MySQL, sur base vierge et historique ; vérifier pagination, contrats snake_case et exemples OpenAPI.
- [ ] Exécuter les tests .NET ciblés, les tests Vitest concernés, le build client et le validateur i18n.
- [ ] Déployer derrière un feature flag désactivé par défaut ; tester le flux en données fictives et former les opérateurs.
- **Sortie :** pilote non transactionnel exploitable, journalisé et limité aux membres/corridors convenus.

### Phase 5 — Canal autorisé et opérations réelles (projet distinct soumis à approbation)

À démarrer uniquement après approbation juridique/partenaire et revue des risques :

- [ ] Sélectionner et contractualiser un prestataire autorisé pour le corridor et le canal.
- [ ] Définir les responsabilités contractuelles, le règlement des échecs, remboursements, erreurs de bénéficiaire et réclamations.
- [ ] Intégrer les tarifs et frais du fournisseur, création/référence d’opération, statuts, webhooks signés, idempotence et rapprochement.
- [ ] Vérifier les callbacks et références côté serveur ; concevoir les reprises et délais d’attente sans produire de faux statut de succès.
- [ ] Renforcer la surveillance, les limites configurables, la revue humaine et les procédures d’escalade avant l’activation.
- [ ] Refaire les analyses juridiques, sécurité, confidentialité et tests opérationnels avant d’activer toute transaction réelle.

## 5. Cycle de vie et invariants

Cycle nominal :

```text
DRAFT → SUBMITTED → UNDER_REVIEW → APPROVED → MATCHING
→ HELPER_SELECTED → WITNESS_PENDING → MEETING_SCHEDULED
→ CASH_HANDOVER_CONFIRMED → TRANSFER_IN_PROGRESS → TRANSFER_SENT
→ BENEFICIARY_CONFIRMED → COMPLETED
```

États exceptionnels :

```text
REJECTED, CANCELLED, EXPIRED, DISPUTED, SUSPENDED,
REFUND_PENDING, REFUNDED, ESCALATED
```

Invariants à faire respecter côté serveur :

- Une demande sous revue, suspendue, rejetée ou expirée n’est jamais proposée aux aidants.
- Une demande comporte un seul bénéficiaire confirmé et un seul aidant assigné pour le MVP ; aucun fractionnement.
- Le témoin est vérifié, distinct du demandeur, de l’aidant et du bénéficiaire, et a accepté la mission.
- La remise n’est confirmée qu’après les confirmations requises de présence et de remise.
- La preuve de transfert et la confirmation du bénéficiaire sont des étapes distinctes de la remise d’espèces.
- Une confirmation partielle ou un litige empêche la clôture automatique.
- `COMPLETED` n’est possible qu’après validation des contrôles requis, de la remise, de la preuve, de la réception et de l’absence de revue/litige ouvert.
- Aucune transition directe de `MATCHING` à `COMPLETED` ; toute transition interdite renvoie un conflit explicite et est journalisée si pertinente.

## 6. Risques et contrôles à couvrir

| Risque | Contrôle produit/technique |
|---|---|
| Fraude ou usurpation d’identité | Vérification proportionnée, MFA/OTP selon rôle, revue des changements de bénéficiaire |
| Exposition de coordonnées ou documents | Accès par rôle/participant, masquage, chiffrement, URLs temporaires, journaux sans secret |
| Contournement des plafonds par fractionnement | Pas de fractionnement, détection des demandes répétées/agrégées, revue humaine |
| Compte financier appartenant à un tiers | Vérification de propriété exigée selon corridor ; blocage si non déclaré |
| Faux justificatif ou rejeu | Empreinte de fichier, détection de réutilisation, preuve référencée au fournisseur, audit |
| Faux témoin ou conflit d’intérêts | Identité distincte, déclaration de conflit, consentement et confirmation séparés |
| Erreur de transfert ou bénéficiaire | Vérification avant remise, interdiction de modifier le bénéficiaire ensuite, incident audité |
| Défaillance d’OTP/idempotence | Limitation des tentatives, expiration, usage unique et idempotence serveur |
| Perte de preuve ou conservation excessive | Politique de rétention documentée et approuvée, sauvegarde et accès tracé |
| Accès inter-tenant ou rôle trop permissif | Contrôles serveur systématiques et tests négatifs d’autorisation |
| Activité assimilable à un service réglementé | MVP sans transfert, validation spécialisée et usage d’un partenaire autorisé avant extension |

## 7. Critères d’acceptation de livraison du MVP

- Un demandeur éligible peut créer, soumettre et suivre une demande sans qu’Expatriate ne reçoive ni ne transfère de fonds.
- Une demande sous revue n’est pas visible dans les demandes disponibles.
- Seuls les aidants éligibles voient des informations anonymisées et peuvent proposer leurs services.
- L’affectation d’un aidant et d’un témoin indépendant exige leur consentement et les vérifications requises.
- Les confirmations de remise, preuves et confirmations de bénéficiaire sont attribuées à leurs auteurs, horodatées et auditées.
- Une opération n’est pas terminée si une étape obligatoire manque, si la réception est partielle ou si un incident/revue est ouvert.
- Les accès aux demandes, champs sensibles et fichiers sont limités au rôle et aux participants autorisés ; aucun accès inter-tenant n’est possible.
- Les erreurs métier sont explicites, les opérations répétées ne créent pas de doubles effets, les listes sont paginées et les API respectent le format snake_case du projet.
- Les parcours demandeur, aidant, témoin et conformité fonctionnent sur mobile et sont disponibles en français et en anglais.
- Build frontend, tests pertinents backend/frontend, migrations MySQL sur bases vierge et historique et validation de parité i18n réussissent.
- Le module est désactivé par défaut et les écrans/contrats n’affirment pas qu’Expatriate est une banque, un portefeuille, un bureau de change ou un opérateur de transfert.

## 8. Livrables

1. Décisions de cadrage juridique et opérationnel, corridor et critères d’éligibilité approuvés.
2. Diagramme des transitions et contrat API validés.
3. Migration, entités, configurations EF, handlers, endpoints, permissions et tests backend.
4. Fonctionnalité Angular complète, traductions FR/EN, tests et parcours par rôle.
5. Documentation OpenAPI, procédure de revue/suspension/incident et consignes de support.
6. Rapport de validation du pilote non transactionnel et décision formelle d’autoriser ou non la phase d’intégration financière.

## 9. Plan d’implémentation ordonné

Ce découpage transforme les phases précédentes en lots de travail vérifiables. Il constitue une proposition : **ne pas commencer les modifications du dépôt avant approbation explicite de ce plan**. Chaque lot dépend de la validation de ses prérequis.

### Lot 0 — Approuver le périmètre et lever les décisions bloquantes

**Pré requis :** aucun.  
**Travail :**

1. [x] Confirmer le MVP non transactionnel et l’exclusion de tout mouvement de fonds réel.
2. [x] Fixer le cloisonnement par tenant et la revue par `org_admin`/`auditor` avec permissions dédiées.
3. Fixer les états initiaux et les règles métier à livrer, y compris annulation, suspension, incident et clôture.
4. Fixer les informations nécessaires à la demande et au bénéficiaire, les règles de visibilité et les pièces attendues.
5. [x] Confirmer que l’intégration du fournisseur et les remboursements automatisés sont hors de cette première implémentation.
6. Confirmer les choix de versions : versions réelles du dépôt ou mise à niveau séparée, sans mélanger une migration de stack au module.

**Livrable :** périmètre approuvé, décisions consignées dans ce document ou dans le contrat API.  
**Critère de sortie :** aucune décision bloquante sur l’identité des acteurs, l’accès aux données, les transitions et le MVP ne reste implicite.

### Lot 1 — Spécifier workflow, données et contrats

**Dépend de :** Lot 0.  
**Travail :**

1. Écrire la table de transitions `état courant × commande → état suivant / erreur`.
2. Définir les agrégats et relations nécessaires, les champs obligatoires, les données personnelles et leur durée de rétention.
3. Définir les DTO de chaque cas d’usage, le JSON snake_case, la pagination, les codes d’erreur et les exemples de réponses.
4. Définir la matrice acteur × permission × action × champ/pièce visible, incluant le contrôle tenant côté serveur.
5. Fixer les garanties d’idempotence, la gestion de concurrence et les événements d’audit.
6. Définir les critères d’acceptation des parcours demandeur, aidant, témoin et conformité.

**Livrable :** contrat API et matrice de règles/accessibilité approuvés avant création de migration.  
**Critère de sortie :** chaque route et chaque transition correspondent à un scénario d’acceptation testable.

### Lot 2 — Socle backend et persistance

**Dépend de :** Lot 1.  
**Travail :**

1. Ajouter les entités et types de domaine minimaux dans `server/Domain/`.
2. Ajouter les configurations EF Core explicites et contraintes/index dans `server/Infrastructure/Persistence/Configurations/`, puis les DbSet dans `AppDbContext`.
3. Créer la migration avec noms de tables et colonnes snake_case, UUID compatibles, contraintes et index adaptés aux recherches et vérifications d’unicité.
4. Vérifier la migration sur MySQL ; examiner et valider les types et opérations sur la version de production sans ajouter de dépendance spécifique au domaine.
5. Ajouter les tests de mapping, contraintes, relations et application de migration.

**Livrable :** schéma persisté et migration réversible/traçable selon les conventions du dépôt.  
**Critère de sortie :** migration appliquée sur des bases MySQL de test vierge et historique ; aucune règle de persistance dépendante du provider dans Domain/Application.

### Lot 3 — Règles métier et cas d’usage backend

**Dépend de :** Lot 2.  
**Travail :**

1. Implémenter les règles de transition dans un point métier central afin que les endpoints ne puissent pas contourner le workflow.
2. Implémenter les premiers cas d’usage : créer/modifier brouillon, soumettre, revue, lister les demandes disponibles anonymisées et lire les demandes autorisées.
3. Ajouter les validateurs, contrôles d’éligibilité et vérification du cloisonnement dans les handlers ou services d’application.
4. Implémenter ensuite les offres, sélection de l’aidant, invitation/acceptation du témoin et déclaration de conflit d’intérêts.
5. Implémenter confirmations de présence/remise, preuves déclaratives, confirmation du bénéficiaire, annulation et suspension.
6. Ajouter idempotence/concurrence aux mutations sensibles ; audit acteur/date/motif/corrélation ; exclure secrets et coordonnées complètes des logs.
7. Ajouter des tests unitaires pour chaque transition permise/interdite et les cas de refus.

**Livrable :** cas d’usage testés indépendamment de HTTP.  
**Critère de sortie :** aucune commande ne permet une transition invalide, une double confirmation ou une clôture en présence d’une étape requise manquante.

### Lot 4 — API, permissions et ressources sensibles

**Dépend de :** Lot 3.  
**Travail :**

1. Ajouter les permissions et leur rattachement explicite aux rôles existants ; ne pas supposer l’existence de rôles métier qui ne sont pas dans le dépôt.
2. Ajouter les endpoints de la section 2.3, leur validation, réponses et documentation OpenAPI.
3. Vérifier l’autorisation à chaque opération et ressource, y compris les lectures par identifiant et les opérations d’administration.
4. Implémenter le masquage des champs, la pagination, les erreurs standard et les codes HTTP définis.
5. Réutiliser le stockage existant uniquement après vérification de son accès ; ajouter pièces avec autorisation, URLs temporaires, validation de type/taille, empreintes et protections contre le rejeu.
6. Déclencher les notifications nécessaires via le système existant, avec contenu minimal.
7. Ajouter tests API de rôles, cas négatifs, accès inter-tenant, DTO snake_case et format d’erreurs.

**Livrable :** API utilisable et documentée par le client, sans transfert financier.  
**Critère de sortie :** tests vérifiant explicitement les permissions, masquages, réponses paginées et absence de fuite inter-tenant réussis.

### Lot 5 — Modèles et intégration frontend

**Dépend de :** contrat stabilisé du Lot 1 ; les écrans connectés dépendent du Lot 4.  
**Travail :**

1. Créer le modèle TypeScript et le service HTTP de `transfer-assistance`, correspondant exactement aux DTO backend.
2. Ajouter l’alias TypeScript du domaine si les imports le justifient et intégrer les routes protégées au shell existant.
3. Ajouter les clés FR/EN aux deux fichiers i18n en respectant le validateur de parité.
4. Construire d’abord le parcours demandeur : brouillon, soumission, liste et suivi.
5. Construire ensuite la file anonymisée et l’offre aidant, puis l’affectation et le parcours témoin.
6. Construire la file de revue conformité et les actions avec motifs et confirmations explicites.
7. Ajouter états chargement, erreur, vide, conflit, expiration, absence de permission, responsive et accessibilité.
8. N’exposer aucune donnée sensible dans les caches ou stockages non protégés ; ne jamais considérer le guard frontend comme une autorisation suffisante.

**Livrable :** interface en français et anglais connectée aux endpoints, avec parcours distincts par rôle.  
**Critère de sortie :** le parcours nominal non transactionnel et les scénarios d’erreur sont testés au niveau composant/service et par un test de parcours pertinent.

### Lot 6 — Validation intégrée, sécurité et pilote contrôlé

**Dépend de :** Lots 2 à 5.  
**Travail :**

1. Exécuter les tests backend ciblés, tests frontend ciblés, build client et validation i18n.
2. Exécuter les tests d’intégration du workflow avec scénarios de double requête, concurrence, annulation, preuve réutilisée, réception partielle, suspension et incident.
3. Effectuer une revue des droits, du masquage, du stockage, de la journalisation et de la rétention des données.
4. Vérifier le schéma et les migrations MySQL ; contrôler la documentation et les exemples API.
5. Activer le module uniquement derrière un feature flag désactivé par défaut, sur données fictives et avec accès opérateur limité.
6. Documenter procédure de support, revue manuelle, suspension, signalement et réponse à incident.

**Livrable :** rapport de validation et décision d’ouverture du pilote non transactionnel.  
**Critère de sortie :** critères de la section 7 atteints ; aucun défaut bloquant d’autorisation, fuite de données ou incohérence d’état.

### Lot 7 — Intégration financière ultérieure, approbation séparée obligatoire

**Dépend de :** pilote non transactionnel validé, validation juridique/opérationnelle et contrat avec un prestataire autorisé.  
Ce lot n’est **pas inclus** dans l’approbation du MVP. Il nécessite une autorisation distincte avant tout développement ou activation. Il couvrira l’intégration fournisseur, webhooks signés, rapprochement, gestion des échecs et remboursements, tests de bout en bout et procédure de mise en production.

## 10. Demande d’approbation

Le Lot 0 et le **plan d’implémentation des Lots 1 à 6** ont été approuvés pour réaliser le MVP non transactionnel. Ces approbations n’autorisent ni mouvement de fonds réel, ni intégration financière du Lot 7. Aucun code n’a été modifié avant leur obtention. Les décisions de conception doivent être consignées avant chaque migration ou activation opérationnelle.

## 11. Point d’avancement de l’implémentation

### Livré dans le premier incrément

- Création des demandes en brouillon, validation, soumission, liste/détail personnel, annulation avant la remise et file de revue par tenant.
- Approbation vers l’état `matching`, rejet ou suspension motivés ; contrôles d’éligibilité du membre, prévention des demandes ouvertes en double et interdiction d’auto-revue.
- Profil volontaire aidant/témoin, revue manuelle distincte pour chaque rôle, MFA selon `TransferAssistance:RequireMfa` et attestation documentée ; aucune pièce d’identité n’est stockée.
- Sélection du bénéficiaire parmi les membres actifs avec e-mail vérifié du même tenant ; la recherche ne retourne qu’un petit nombre de résultats et exclut le demandeur.
- Permissions dédiées, audit des mutations et cloisonnement par tenant.
- Interface Angular FR/EN pour les demandes, leur revue et la vérification des profils participants.
- Entités EF Core et migrations MySQL pour les demandes et profils.

### Ajouts du workflow MVP

- Mise en relation anonymisée : file des demandes éligibles pour aidants vérifiés, dépôt d’offres et sélection par le demandeur.
- Invitation, acceptation/refus et déclaration de conflit du témoin, avec contrôle d’indépendance.
- Planification, confirmations séparées de remise et attestation séparée du témoin.
- Enregistrement déclaratif d’une référence de transfert externe ; aucune preuve documentaire ni opération financière n’est exécutée ou conservée par Expatriate.
- Confirmation authentifiée par le bénéficiaire sélectionné. Une réception complète clôt le dossier ; une réception partielle ou absente ouvre un litige.
- Suspension/reprise et résolution des litiges par la conformité, journal d’événements, notifications in-app, concurrence optimiste et expiration périodique.
- Interfaces Angular pour les rôles demandeur, aidant, témoin, bénéficiaire et conformité ; feature flag désactivé par défaut côté client et serveur.
- Migration du workflow générée avec reprise des clés des demandes ouvertes historiques. Sa relecture a identifié un défaut : le filtre des états ouverts porte sur la sous-requête mais pas sur les lignes modifiées ; des dossiers fermés peuvent recevoir la même clé et empêcher la création de l’index unique. La correction et le traitement des groupes historiques ambigus sont prévus en section 12.

### Validation et prérequis avant pilote

- Le build backend et le build production Angular réussissent ; la validation de parité i18n réussit. Le bundle Angular dépasse toujours le budget initial préexistant de 500 kB (706,39 kB).
- Les validations de la première tranche ont donné 59 tests backend hors MySQL et 38 tests frontend réussis, sans exclusion temporaire. Les quatre constructions du handler paiement et la fixture d’ownership ont été corrigées ; un cas distinct vérifie le membre inexistant. Six tests d’intégration MySQL sont explicitement ignorés localement en l’absence d’instance dédiée, conformément à la décision de les exécuter uniquement en CI pour le moment.
- La migration a été générée et relue, mais n’a pas été appliquée ni validée sur une base MySQL de test représentative. MySQL est désormais l’unique cible ; SQLite n’est pas un prérequis de livraison.
- Le module reste désactivé. Pour un environnement de test, activer explicitement le feature flag serveur et son équivalent client, et définir un `MaximumRequestAmountMur` strictement positif ; sa valeur par défaut `0` bloque les demandes. Ne pas choisir de plafond métier sans décision produit/juridique.
- Avant pilote : faire passer les tests workflow et d’accès inter-tenant, appliquer la migration sur un jeu de données représentatif, vérifier les permissions en HTTP et conduire les revues juridique, opérationnelle et de sécurité. Aucun mouvement de fonds réel ni intégration financière n’est autorisé.

## 12. Plan d’application des écarts avant production

### 12.1 Objectif, limites et statut

Rendre exploitable en production **l’assistance communautaire non transactionnelle** : dossiers, accréditations, coordination, déclarations et litiges. La référence externe reste déclarative et ne vaut pas confirmation du prestataire.

- **MySQL uniquement** : utiliser la même version, collation, configuration de fuseau horaire et mode SQL en intégration/préproduction qu’en production. Aucun lot SQLite.
- Aucun mouvement de fonds, connecteur financier, webhook prestataire, rapprochement ou remboursement. Le Lot 7 reste soumis à une approbation séparée.
- La MFA reste optionnelle selon la décision MVP existante. Une ouverture sans MFA exige une acceptation explicite du risque ; si la gouvernance exige la MFA, son implémentation complète devient un prérequis distinct. Un booléen utilisateur ne suffit pas.
- La première tranche PROD-0 à PROD-2 a été approuvée le 22 octobre 2026. Cette approbation n’est ni une autorisation d’activation ni une déclaration de conformité ; PROD-3 à PROD-6 restent nécessaires avant ouverture.
- Les conventions historiques mentionnant SQLite devront être alignées sur la décision MySQL uniquement, sans refactorisation générale ni changement de stack opportuniste.

### 12.2 Synthèse des lots et dépendances

Les priorités indiquent l’ordre de traitement, pas une dispense de validation : les critères de sortie de tous les lots applicables doivent être satisfaits avant ouverture.

| Lot | Priorité | Objet | Dépendances |
|---|---|---|---|
| PROD-0 | P0 | Fixer les décisions de mise en production et la cible MySQL | Première tranche approuvée ; certaines décisions restent attendues |
| PROD-1 | P0 | Fiabiliser la migration et les données historiques | PROD-0 |
| PROD-2 | P0 | Rétablir une validation automatisée standard | PROD-0 |
| PROD-3 | P0 | Valider le workflow réel, ses droits et sa concurrence | PROD-1 et PROD-2 |
| PROD-4 | P1 | Terminer la robustesse frontend et la recette navigateur | PROD-2 ; recette intégrée après PROD-3 |
| PROD-5 | P1 | Fiabiliser l’exploitation, les notifications et l’expiration | PROD-0 ; validation intégrée après PROD-3 |
| PROD-6 | P0 | Préproduction, restauration, pilote et décision d’ouverture | PROD-1 à PROD-5 et validations opérationnelles |

### PROD-0 — Décisions de livraison et configuration

**Travaux**

1. Consigner la version exacte de MySQL et la matrice .NET/EF Core/Pomelo supportée ; conserver les versions actuelles sauf incompatibilité démontrée.
2. Aligner les références documentaires pertinentes sur MySQL uniquement. L’absence de prise en charge SQLite n’est plus un défaut à corriger.
3. Faire approuver le plafond métier, les délais de mise en relation/invitation et les règles d’éligibilité ; conserver le plafond bloquant tant que la décision n’est pas prise.
4. Définir les tenants autorisés au pilote, les responsables conformité/support et la procédure d’accréditation. Utiliser des comptes opérateurs distincts des participants.
5. Faire valider la politique MFA : maintien optionnel avec risque accepté ou chantier MFA complet séparé, sans activation artificielle de `MfaEnabled`.
6. Fixer les durées de rétention des dossiers, références externes, événements et journaux, ainsi que les règles d’archivage/purge compatibles avec l’audit.
7. Définir avant les essais les volumes représentatifs, le pic de concurrence, les objectifs de latence p95, les délais de notification/expiration et les objectifs de restauration RPO/RTO.
8. Documenter les configurations client/serveur par environnement, la gestion des secrets et la désactivation d’urgence. Vérifier que les identifiants de démonstration et quick-login ne sont pas exposés en production.

**Critères de sortie**

- Fiche de décisions approuvée, avec responsables et valeurs explicites ; aucune valeur métier choisie arbitrairement par le développement.
- Configurations de préproduction documentées ; production toujours désactivée à ce stade.

### PROD-1 — Migration MySQL et intégrité historique

**Travaux**

1. Corriger la reprise de `open_request_key` pour ne viser que le dossier ouvert unique du couple tenant/demandeur, jamais ses dossiers fermés.
2. Inventorier les états historiques : aucun ouvert, un ouvert avec plusieurs fermés, plusieurs ouverts, dossiers inactifs et clés déjà renseignées. Définir explicitement quels dossiers comptent pour l’unicité.
3. Détecter les groupes ambigus avant migration ; produire un diagnostic exploitable et les traiter par une décision métier tracée. Ne pas résoudre le problème en supprimant arbitrairement des dossiers ni en laissant des doublons actifs contournant l’unicité.
4. Vérifier si la migration a déjà été appliquée dans un environnement partagé. Si oui, préparer aussi une réparation en avant pour les données existantes ; une simple modification du fichier historique ne répare pas les bases déjà migrées.
5. Valider les index uniques, clés étrangères, jetons de concurrence, longueurs et types MySQL ; vérifier le comportement des erreurs d’unicité et de concurrence.
6. Tester sur une base vierge et une copie anonymisée représentative du schéma précédent, avec les fixtures historiques ci-dessus.
7. Préparer un script de précontrôle et le script de déploiement révisé ; mesurer la durée et les verrous. Tenir compte des commits implicites DDL de MySQL : ne pas promettre un rollback transactionnel global.
8. Prévoir sauvegarde et restauration avant migration ; tester la restauration plutôt qu’un `Down` destructif sur des dossiers réels.

**Critères de sortie**

- Migration réussie sur les deux types de bases ; dossiers fermés sans clé d’ouverture et au plus un dossier ouvert autorisé par tenant/demandeur.
- Comptages et relations conservés, aucun groupe ambigu non traité, contrôles post-migration réussis.
- Réexécution du déploiement sur une base à jour sans altération inattendue ; procédure de récupération répétée en préproduction.

### PROD-2 — Tests standards et garde-fous de livraison

**Travaux**

1. Mettre à jour les quatre constructions de `RecordPaymentCommandHandler` dans les tests de paiement avec un double de `INotificationService`, sans changer le métier paiement.
2. Réparer la fixture du test d’ownership du transfert en utilisant un autre membre réellement éligible ; conserver un test distinct pour le membre inexistant/inéligible.
3. Rendre le lancement ciblé Vitest reproductible avec la configuration du dépôt, sans option corrective ad hoc.
4. Exécuter les tests ciblés après corrections, puis la suite backend/frontend standard pour constituer une référence de livraison sans exclusion temporaire.
5. Ajouter des tests d’intégration avec une base MySQL isolée et éphémère. Ne pas utiliser EF InMemory comme preuve des contraintes ou de la concurrence MySQL.
6. Créer ou compléter le pipeline CI : restauration reproductible, builds, tests unitaires/intégration, parité i18n et publication des résultats. Ne pas réutiliser une base de développement partagée.
7. Examiner les avertissements de dépendances et de build ; corriger les incompatibilités bloquantes ou documenter leur traitement avant ouverture, sans mise à niveau globale non justifiée.

**Critères de sortie**

- Commandes documentées et reproductibles depuis un checkout propre ; zéro exclusion temporaire, zéro test en échec et aucune nouvelle régression.
- CI bloquante en cas d’échec ; résultats MySQL consultables et distincts des résultats unitaires.

### PROD-3 — Contrats HTTP, workflow et concurrence réelle

**Travaux**

1. Tester via HTTP le parcours complet : accréditations → soumission → approbation → offre → sélection → invitation/acceptation témoin → rendez-vous → trois confirmations → référence externe → réception totale.
2. Tester les branches rejet, annulation avant remise, refus/expiration du témoin, retrait d’accréditation, réception partielle/non reçue, litige, suspension/reprise et résolution.
3. Construire une matrice rôle/permission/état pour chaque endpoint. Vérifier notamment `org_admin` exclu des nouvelles missions aidant/témoin, l’interdiction d’auto-revue, l’indépendance du témoin et les missions déjà engagées préservées.
4. Vérifier l’isolation inter-tenant par accès direct aux identifiants, listes, recherches, audit et notifications, y compris les comptes multi-rôles et les changements de tenant autorisés.
5. Sur MySQL, envoyer simultanément : deux soumissions du même demandeur, deux demandes d’accréditation du même rôle, deux sélections, deux réponses à invitation et deux confirmations identiques.
6. Tester les références externes réutilisées selon la portée d’unicité prévue, les mises à jour concurrentes et la course entre expiration et action utilisateur.
7. Vérifier les effets persistés, pas seulement le code HTTP : un seul état valide, aucun doublon d’événement métier ou de notification logique et aucune confirmation perdue. Une répétition doit être idempotente ou produire un conflit explicite conforme au contrat.
8. Contrôler les champs masqués de chaque DTO, pagination, snake_case, erreurs `{ "error": "..." }`, codes HTTP, exemples OpenAPI et cohérence du feature flag sur toutes les routes.

**Critères de sortie**

- Chaque transition permise et interdite de la matrice possède un test ; tous réussissent sur la cible MySQL.
- Aucun accès inter-tenant ou non autorisé ; invariants métier conservés après les tests concurrents.
- Aucun endpoint du module ne contourne la désactivation serveur.

### PROD-4 — Robustesse frontend et recette multi-comptes

**Travaux**

1. Tester puis corriger la reprise des recherches après erreur HTTP : une panne ne doit pas terminer définitivement le flux des recherches témoins/bénéficiaires.
2. Conserver le délai de 1 000 ms des recherches concernées, le minimum de caractères, l’annulation immédiate des requêtes obsolètes et la suppression des anciens résultats.
3. Vérifier les actions en cours : boutons protégés contre le double clic, retour explicite en cas de conflit, rechargement de l’état serveur et absence de faux succès.
4. Vérifier connexion, restauration de session, renouvellement, déconnexion et changement de tenant ; aucune donnée ou confirmation de l’ancien contexte ne doit subsister.
5. Exécuter une recette navigateur avec des comptes distincts demandeur, aidant, témoin, bénéficiaire et opérateur. Couvrir le parcours nominal et les branches principales de PROD-3.
6. Vérifier les messages FR/EN de toutes les erreurs du module, formulaires, dialogues PrimeNG, états vide/chargement/échec, navigation clavier, focus et affichage mobile.
7. Examiner les caches PWA/service worker et le stockage navigateur : aucune réponse métier sensible réutilisée après changement de compte/tenant ni mise en cache non autorisée des API.
8. Mesurer le chargement du module et le bundle. Traiter les régressions attribuables au module ; toute exception au budget doit être motivée et approuvée, pas masquée par une hausse arbitraire.

**Critères de sortie**

- Tests frontend et parcours navigateur réussis ; recherche réutilisable après une erreur et délai de 1 000 ms testé précisément.
- Aucun mélange de comptes, aucun doublon d’action et aucune clé i18n brute dans les scénarios couverts.
- Résultats de recette desktop/mobile et clavier consignés ; budget respecté ou exception acceptée.

### PROD-5 — Exploitation, expiration, notifications et observabilité

**Travaux**

1. Tester le service d’expiration au démarrage, après indisponibilité, à la frontière des échéances et en présence de plusieurs instances API.
2. Traiter explicitement les conflits et erreurs transitoires du traitement périodique avec journalisation, stratégie de reprise bornée et alerte ; ne pas avaler les erreurs ni transformer un échec en succès.
3. Vérifier l’atomicité des mutations et la fiabilité des notifications. Tester panne d’envoi et reprise ; réutiliser les mécanismes existants et n’ajouter une persistance de livraison que si l’existant ne satisfait pas la garantie retenue.
4. Documenter le rôle des préférences de notification : le dossier et l’historique restent consultables même si la notification in-app est désactivée.
5. Mettre en place des logs structurés corrélés et des indicateurs : erreurs HTTP/conflits, durée des actions, dossiers bloqués, retard d’expiration et échecs de notification. Ne pas journaliser de secrets ni de références sensibles complètes.
6. Vérifier la configuration de production : HTTPS, authentification, permissions, CORS, secrets externalisés, limites des recherches/actions et accès aux journaux.
7. Exécuter les tests de charge définis en PROD-0 sur les listes, recherches et transitions ; analyser les requêtes/index avant toute optimisation.
8. Documenter l’exploitation : revue d’accréditation, litige, incident, suspension, dossiers bloqués, rétention et purge sans destruction de l’audit requis.

**Critères de sortie**

- Redémarrage et exécution multi-instance sans mutation perdue ni événement logique dupliqué ; rattrapage des échéances démontré.
- Tests de panne/reprise réussis ; alertes déclenchées lors d’un exercice et reçues par le responsable désigné.
- Objectifs de charge/latence et de délai opérationnel approuvés en PROD-0 atteints.

### PROD-6 — Préproduction, pilote et ouverture contrôlée

**Travaux**

1. Déployer le même artefact validé par CI dans une préproduction MySQL représentative, avec données fictives ou anonymisées.
2. Répéter la sauvegarde, la migration, les contrôles post-déploiement et la restauration ; mesurer les RPO/RTO. Ne pas supposer que désactiver le module annule la migration.
3. Réaliser les revues juridique, opérationnelle et de sécurité avant ouverture ; consigner les décisions et traiter les défauts bloquants. Aucune de ces revues n’est déclarée réalisée par ce plan.
4. Vérifier l’accord explicite sur le plafond, les tenants pilotes, les opérateurs, la rétention et le risque MFA ; ne pas activer si une décision requise manque.
5. Tester la désactivation d’urgence côté serveur et client, et son effet sur les dossiers déjà engagés. Prévoir un traitement opérateur documenté sans réactiver silencieusement les endpoints.
6. Ouvrir d’abord un pilote limité avec surveillance active et procédure de signalement ; définir sa durée et ses seuils d’arrêt avant le démarrage.
7. Après pilote, statuer sur l’ouverture élargie à partir des résultats, incidents, indicateurs et réserves, pas uniquement du succès des builds.

**Critères de sortie**

- Sauvegarde/restauration et désactivation d’urgence testées ; objectifs RPO/RTO atteints.
- Aucun défaut bloquant non corrigé d’autorisation, d’intégrité, de confidentialité ou de disponibilité.
- Accord explicite produit, technique et opérationnel, avec validation juridique et de sécurité consignée.
- Le statut « prêt production » concerne uniquement le périmètre non transactionnel validé ; il n’autorise aucune intégration financière.

### 12.3 Preuves attendues et ordre d’exécution

**Ordre proposé :** approbation → PROD-0 → PROD-1 et PROD-2 → PROD-3 ; préparation de PROD-4/PROD-5 en parallèle lorsque leurs prérequis sont disponibles → recette intégrée → PROD-6.

Les résultats seront consignés lors de l’exécution : commit/artefact testé, version MySQL, commandes et résultats de tests, matrice de recette, contrôles de migration, mesures de charge/restauration, réserves et approbations. Un élément non exécuté reste explicitement « non validé ».

**Première tranche proposée après approbation :** PROD-0 à PROD-2, sans activation en production. Les tranches suivantes restent nécessaires pour atteindre l’objectif de production ; les tests unitaires seuls ne clôturent pas ce plan.

### 12.4 Décisions et réalisation de la première tranche — 22 octobre 2026

#### Décisions confirmées

| Sujet | Décision |
|---|---|
| MySQL de production | `8.0.46-0ubuntu0.24.04.4` |
| Modes SQL | `ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION` |
| Encodage / collation | `utf8mb4` / `utf8mb4_unicode_ci` |
| MFA | Optionnelle, avec acceptation explicite du risque par le demandeur du projet ; ne pas activer artificiellement les profils MFA |
| Plafond et délais métier | À confirmer ; conserver le plafond bloquant actuel et ne pas activer le module |
| Tenants pilotes, responsables, rétention, charge, p95 et RPO/RTO | À confirmer ; prérequis d’ouverture non levés |
| Fuseau horaire / configuration temporelle MySQL | À confirmer ; aucune conversion globale introduite |
| Validation MySQL de cette tranche | CI uniquement pour le moment, sans connexion à une base locale ou partagée |

La CI utilise l’image officielle `mysql:8.0.46`, les modes SQL ci-dessus et des bases `utf8mb4_unicode_ci`. Ce contrôle du moteur ne remplace pas la recette finale sur le paquet Ubuntu exact et la configuration de production.

#### Correctifs et garde-fous implémentés

- Migration historique : contrôle des doublons actifs avant toute nouvelle DDL du workflow ; reprise des clés limitée aux états ouverts actifs. Les dossiers fermés/inactifs ne réservent pas de clé. Un groupe ambigu provoque une erreur explicite identifiant sa clé tenant/demandeur, plutôt qu’une migration silencieusement incomplète.
- Migration de réparation en avant `20261022060000_RepairTransferAssistanceOpenRequestKeys` pour les bases ayant déjà appliqué le workflow : contrôle préalable, nettoyage des clés fermées/inactives et reconstruction des clés ouvertes. Aucun dossier ni historique métier supprimé ; le retour arrière de cette normalisation exige la sauvegarde vérifiée.
- Script de diagnostic [transfer-assistance-preflight.sql](scripts/transfer-assistance-preflight.sql), à lancer avant déploiement ; toute ligne de doublon doit faire l’objet d’une résolution métier tracée.
- Tests standards réparés : double de notifications dans les tests paiement, contrôle d’ownership avec membre existant et éligible, cas distinct pour membre inexistant, racine Vitest corrigée. Aucun retrait supplémentaire de fichier de test.
- Quatre tests sans base vérifient la génération SQL des migrations, le script complet idempotent et la correspondance du modèle EF avec le snapshot.
- Six tests MySQL couvrent base vierge/réexécution, historique ouvert/fermé/inactif, refus des ambiguïtés avant DDL, réparation d’un workflow déjà appliqué, refus d’ambiguïté en réparation, index unique et concurrence optimiste réelle.
- [Pipeline de validation](.github/workflows/validation.yml) : suites backend/frontend, build production Angular, i18n, MySQL éphémère, rapports TRX/JUnit et génération SQL idempotente pour revue. Le job MySQL exige au moins six tests exécutés et réussis, sans tests ignorés.
- Outil EF local fixé à `9.0.7` dans [.config/dotnet-tools.json](.config/dotnet-tools.json) ; version de génération MySQL fixée à `8.0.46` dans la factory design-time.
- Dépendance `Microsoft.OpenApi` explicitement fixée à `2.7.5` pour éliminer l’avertissement NU1903 signalé par la restauration ; le contrôle NuGet des dépendances transitives ne signale plus de paquet vulnérable.
- Conventions DB-CONF v1/v3 et référence de stack v4 alignées sur MySQL uniquement.

#### Commandes reproductibles

Depuis la racine, PowerShell :

```powershell
dotnet tool restore
dotnet test .\server\server.Tests\server.Tests.csproj --filter "Category!=MySqlIntegration"
Set-Location .\client
npm test -- --maxWorkers 1
npm run lint:i18n
npm run build
```

Le job MySQL exécute la sélection `Category=MySqlIntegration`. Pour une exécution future hors CI, fournir `TRANSFER_ASSISTANCE_TEST_MYSQL` via l’environnement sécurisé, vers une **instance dédiée**, sans propriété `Database`. Les tests créent leurs propres bases au nom aléatoire et ne suppriment que celles qu’ils ont créées. Le compte de test doit pouvoir créer/supprimer ces bases ; ne jamais utiliser une instance de production. Sans cette variable, les six tests sont signalés comme ignorés, pas comme validés.

Pour préparer le SQL de déploiement, depuis la racine :

```powershell
dotnet tool restore
Set-Location .\server
dotnet ef migrations script --idempotent --output "$env:TEMP\exp365-migrations-review.sql"
```

La génération lit la configuration design-time et ne doit pas être confondue avec l’application de la migration. Le script généré doit être relu et testé avant usage ; la CI en conserve une copie dans ses résultats backend.

#### Procédure de migration à répéter en préproduction

1. Désactiver le module et arrêter toutes les instances API, traitements d’expiration et autres écritures ; le feature flag seul n’arrête pas les traitements de fond. Ne pas laisser plusieurs instances appliquer les migrations simultanément.
2. Vérifier la sauvegarde et sa restauration, puis exécuter le script de diagnostic sur la base concernée. Ne pas poursuivre si des dossiers ouverts actifs sont en doublon.
3. Vérifier `__EFMigrationsHistory` et les colonnes/index présents. Si une ancienne migration a partiellement échoué, MySQL peut avoir conservé des DDL : ne pas relancer aveuglément. Restaurer la sauvegarde ou préparer une réparation contrôlée après inspection.
4. Appliquer le script validé en maintenance, ou laisser une seule instance effectuer la migration au démarrage selon le mécanisme existant. Ne pas modifier manuellement l’historique EF pour masquer un échec.
5. Contrôler les comptages, les relations, les clés et l’absence de doublons ; vérifier que la migration de réparation apparaît dans l’historique.
6. Reprendre les instances et traitements après les contrôles ; garder l’assistance désactivée et son plafond à `0` jusqu’aux décisions et validations de PROD-3 à PROD-6.

#### Résultats obtenus et réserves

- Backend : **59 tests hors MySQL réussis en Release, 0 échec** ; six tests MySQL réservés à la CI, signalés comme ignorés lors du lancement local sans connexion. Aucune exclusion temporaire et snapshot EF conforme. Une sortie temporaire a évité les conflits avec les fichiers du serveur de développement.
- Frontend : **38 réussis sur les quatre fichiers de la suite configurée**, dont 18 sur le module transfert. Des délais d’initialisation de workers Vitest ont affecté les premières tentatives locales ; la suite complète a ensuite réussi avec un seul worker.
- Build Angular et parité i18n : réussis. Avertissement préexistant conservé : bundle initial `706,25 kB` pour un budget d’avertissement de `500 kB` ; aucun relèvement de seuil.
- Compilation backend réussie ; avertissement préexistant `CS9113` sur `BulkImportMembersCommand` conservé.
- Syntaxe et expressions du workflow GitHub vérifiées avec `actionlint 1.7.7` ; aucun défaut signalé après correction.
- **MySQL non exécuté localement**, conformément au choix explicite de validation en CI. Le workflow est préparé, mais aucun résultat d’exécution GitHub n’est revendiqué avant son lancement après publication.
- PROD-0 reste partiellement bloqué par les décisions métier/opérationnelles manquantes. PROD-1 reste à valider sur MySQL et sur un historique représentatif ; PROD-2 reste à confirmer par une CI verte.
- Aucune migration appliquée sur une base partagée, aucun mouvement de fonds et aucune activation client/serveur effectués.
