# Guide Git de l'équipe

## Modèle de branches

| Branche | Rôle | Écriture |
|---|---|---|
| `main` | Version stable étiquetée | Via PR de `develop`, fusion par Dev 1 en fin de jalon |
| `develop` | Intégration, toujours compilable | Uniquement via Pull Request |
| `feature/<module>-<desc>` | Une tâche | Son auteur |
| `fix/<desc>` | Correction | Son auteur |
| `hotfix/<desc>` | Urgence sur `main` | Dev 1 |

Préfixes de modules : `core`, `sec`, `emp`, `cli`, `ord`, `del`, `auction`, `cat`, `pay`, `loy`, `rpt`, `ui`, `i18n`, `portal`, `not`, `stk`, `sup`, `jobs`.

## Cycle quotidien

```bash
# 1. Matin : se mettre à jour
git switch develop
git pull

# 2. Créer sa branche
git switch -c feature/ord-qr-generation

# 3. Travailler : petits commits fréquents
git add src/NyangoPress.Domain/Orders/
git commit -m "feat(orders): ajoute la génération de QR code par article"

# 4. Avant de pousser : se synchroniser et vérifier
git fetch origin
git rebase origin/develop
dotnet build
dotnet test

# 5. Pousser et ouvrir la PR
git push -u origin feature/ord-qr-generation
gh pr create --base develop --fill        # ou via le site GitHub
```

Après approbation et CI verte : **Squash and merge**, puis :

```bash
git switch develop
git pull
git branch -d feature/ord-qr-generation
```

## Conventions de commit

Format : `type(module): description à l'impératif, en français`

| Type | Usage |
|---|---|
| `feat` | Nouvelle fonctionnalité |
| `fix` | Correction de bug |
| `docs` | Documentation |
| `test` | Tests |
| `refactor` | Réorganisation sans changer le comportement |
| `style` | Format uniquement |
| `chore` | CI, outils, dépendances |

## Résoudre un conflit

1. `git fetch origin && git rebase origin/develop`
2. Git s'arrête sur le premier conflit : ouvrez le fichier, cherchez `<<<<<<<`.
3. Gardez ce qui doit l'être **en comprenant les deux versions** ; en cas de doute, appelez l'autre auteur.
4. `git add <fichier>` puis `git rebase --continue`.
5. Compilez et testez. Poussez avec `git push --force-with-lease` (jamais `--force`, et seulement sur **votre** branche).

Annuler le rebase en cas de panique : `git rebase --abort`.

## Migrations de base de données (règle spéciale)

Les migrations parallèles provoquent des conflits dans le `ModelSnapshot`.

1. Rebasez sur `develop` **juste avant** de créer la migration.
2. Nommez avec le module : `dotnet ef migrations add Orders_AddItemPhotos --project src/NyangoPress.Infrastructure --startup-project src/NyangoPress.Api`
3. Fusionnez la PR dans l'heure.
4. En cas de conflit sur le snapshot : `dotnet ef migrations remove`, `git rebase origin/develop`, recréez la migration.
5. Ne modifiez jamais le snapshot à la main. Annoncez « migration en cours » dans le groupe.

## Commandes utiles

| Besoin | Commande |
|---|---|
| Voir l'état | `git status` |
| Voir l'historique | `git log --oneline --graph --decorate -20` |
| Annuler un fichier non commité | `git restore <fichier>` |
| Défaire le dernier commit (garde les fichiers) | `git reset --soft HEAD~1` |
| Annuler une fusion déjà poussée | `git revert <hash>` |
| Mettre de côté | `git stash` puis `git stash pop` |
| Changer de branche avec travail en cours | `git stash` puis `git switch <branche>` |

## À ne jamais faire

- `git push --force` sur `develop` ou `main`
- Commiter `bin/`, `obj/`, `.vs/`, `docker/.env`, secrets, clés d'API
- Laisser une branche vivre plus d'une journée sans la synchroniser
- Fusionner sa propre PR sans relecture
