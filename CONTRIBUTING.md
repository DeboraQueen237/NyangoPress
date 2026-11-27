# Contribuer à NyangoPress

Merci de respecter ces règles, elles évitent les conflits et protègent la qualité.

## Règles d'or
1. Jamais de commit direct sur `main` ni `develop` : tout passe par une Pull Request.
2. `develop` compile et passe les tests en permanence.
3. Une branche = une tâche = une petite PR (moins de 400 lignes si possible).
4. Chaque matin : `git switch develop && git pull`, puis mettez votre branche à jour.
5. Modifiez uniquement les dossiers de votre module (voir `.github/CODEOWNERS`).
6. Aucun secret dans Git (User Secrets, fichier `.env` ignoré).
7. Aucun texte en dur dans l'interface : ressources FR et EN.
8. Les calculs (prix, droits, règles) sont côté serveur uniquement.
9. Chaque endpoint est protégé par défaut.
10. Vous comprenez ce que vous commitez, y compris le code généré par IA.

## Branches
`feature/<module>-<description>` · `fix/<description>` · `hotfix/<description>`

## Commits (Conventional Commits)
`feat(orders): ...` · `fix(pricing): ...` · `docs: ...` · `test: ...` · `refactor: ...` · `chore: ...`

## Pull Request
- Cible : `develop`. Utiliser le modèle fourni.
- CI verte + au moins 1 approbation. Fusion en **Squash and merge**.
- Relecture des PR des autres sous 2 h.

Détails : [docs/GUIDE_GIT.md](docs/GUIDE_GIT.md) et [docs/CONVENTIONS.md](docs/CONVENTIONS.md).
