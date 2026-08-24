# SCHOOL365 - Conventions et Décisions Complémentaires (FR)

## Modifications extraites du cahier des charges

### Gestion des établissements
- Un établissement peut être créé en répliquant la configuration d'un autre établissement (mode template/copie).

### Modèle de journée académique
- Le système doit vérifier que la somme des périodes générées et des pauses est cohérente avec la plage définie entre l'heure de début et l'heure de fin de journée.
- Lors de la configuration du premier jour, le système peut proposer d'appliquer la même configuration aux autres jours de la semaine.
- Toute modification du modèle de journée académique doit régénérer les périodes.
- Si des emplois du temps existent déjà et sont utilisés par l'établissement, la modification doit être bloquée ou nécessiter une réinitialisation explicite.

### Paiements
- Les modes de paiement supportés incluent également : cash.

## Convention DB-CONF — Gestion des fournisseurs de bases de données
- SQLite et MySQL sont supportés.
- SQLite est le fournisseur par défaut.
- Le fournisseur est sélectionné via le fichier de configuration.
- Aucun changement de code métier ne doit être nécessaire pour passer de SQLite à MySQL.
- Les couches Domain et Application ne doivent pas dépendre du moteur de base de données.
- Les migrations EF Core doivent rester compatibles SQLite et MySQL.
- Les paramètres de connexion doivent être externalisés.
- Le code spécifique au fournisseur doit être isolé dans Infrastructure.
- SQLite et MySQL peuvent coexister dans la solution mais seul le fournisseur configuré est activé à l’exécution.
- Le projet doit être livré avec une configuration SQLite fonctionnelle par défaut.

## Conventions techniques définitives
- Tout le code source doit être rédigé exclusivement en anglais.
- Backend : conventions .NET (PascalCase, interfaces préfixées par I).
- Frontend : conventions Angular (fichiers en kebab-case).
- API JSON : snake_case.
- Base de données : tables et colonnes en snake_case.
- DTOs : Request/Response naming pattern.
- Endpoints REST : ressources au pluriel.
