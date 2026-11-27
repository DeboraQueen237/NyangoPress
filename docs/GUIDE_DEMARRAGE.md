# Guide de démarrage : du zéro au premier lancement

Ce guide est suivi **dans l'ordre**. Les étapes 1 et 2 concernent tout le monde. Les étapes 3 à 7 sont faites **une seule fois par Dev 1**. L'étape 8 concerne les 4 autres développeurs. Comptez 2 à 3 heures le jour 1.

> Les commandes sont données pour un terminal (PowerShell sous Windows, Terminal sous macOS ou Linux). Vérifiez les options avec `dotnet new <modèle> --help` si votre SDK affiche une différence.

---

## Étape 1. Installer les prérequis (tout le monde)

| Outil | Pourquoi | Vérification |
|---|---|---|
| **.NET 10 SDK** | Compiler et lancer | `dotnet --version` affiche `10.x.x` |
| **Git** | Versionner | `git --version` |
| **Docker Desktop** | PostgreSQL, pgAdmin, Mailpit | `docker --version` |
| **Éditeur** : Visual Studio 2026, Rider ou VS Code + extension *C# Dev Kit* | Coder | Ouvre un projet C# |
| **GitHub CLI** (facultatif) | Créer le dépôt et les PR en ligne de commande | `gh --version` |
| **Node.js LTS** (facultatif) | Outils front, Playwright | `node --version` |
| **mkcert** (facultatif) | Certificat HTTPS local de confiance | `mkcert -version` |

Installer l'outil EF Core (une fois par machine) :

```bash
dotnet tool install --global dotnet-ef
```

Configurer Git avec votre identité (tout le monde) :

```bash
git config --global user.name "Votre Nom"
git config --global user.email "vous@exemple.com"
git config --global pull.rebase true
git config --global init.defaultBranch main
```

## Étape 2. Comptes à créer (tout le monde)

1. Un compte **GitHub** avec la **double authentification activée**.
2. Communiquez votre nom d'utilisateur GitHub à Dev 1.
3. Un groupe de discussion (WhatsApp ou Discord) pour l'équipe.

## Étape 3. Créer le dépôt GitHub (Dev 1)

**Option A : sur le site github.com**
1. *New repository* → nom `NyangoPress` → **Private** → ne cochez ni README ni .gitignore.
2. *Settings → Collaborators* : invitez les 4 autres développeurs (droit *Write*).

**Option B : en ligne de commande**
```bash
gh auth login
gh repo create NyangoPress --private --description "Plateforme de gestion de pressing"
```

## Étape 4. Créer la solution (Dev 1)

```bash
mkdir NyangoPress && cd NyangoPress
git init -b main
dotnet new gitignore
dotnet new globaljson --roll-forward latestFeature
dotnet new sln -n NyangoPress          # .NET 10 crée NyangoPress.slnx

# Couches
dotnet new classlib -n NyangoPress.Domain         -o src/NyangoPress.Domain
dotnet new classlib -n NyangoPress.Application    -o src/NyangoPress.Application
dotnet new classlib -n NyangoPress.Infrastructure -o src/NyangoPress.Infrastructure
dotnet new classlib -n NyangoPress.Shared         -o src/NyangoPress.Shared
dotnet new webapi   -n NyangoPress.Api            -o src/NyangoPress.Api
dotnet new blazor   -n NyangoPress.Web            -o src/NyangoPress.Web --interactivity Server --all-interactive

# Tests
dotnet new xunit -n NyangoPress.UnitTests        -o tests/NyangoPress.UnitTests
dotnet new xunit -n NyangoPress.IntegrationTests -o tests/NyangoPress.IntegrationTests

# Ajouter tous les projets à la solution
dotnet sln add src/NyangoPress.Domain src/NyangoPress.Application src/NyangoPress.Infrastructure src/NyangoPress.Shared src/NyangoPress.Api src/NyangoPress.Web tests/NyangoPress.UnitTests tests/NyangoPress.IntegrationTests
```

Références entre projets (respectent les règles de dépendance) :

```bash
dotnet add src/NyangoPress.Application    reference src/NyangoPress.Domain src/NyangoPress.Shared
dotnet add src/NyangoPress.Infrastructure reference src/NyangoPress.Application
dotnet add src/NyangoPress.Api            reference src/NyangoPress.Application src/NyangoPress.Infrastructure src/NyangoPress.Shared
dotnet add src/NyangoPress.Web            reference src/NyangoPress.Shared
dotnet add tests/NyangoPress.UnitTests        reference src/NyangoPress.Domain src/NyangoPress.Application
dotnet add tests/NyangoPress.IntegrationTests reference src/NyangoPress.Api
```

Paquets NuGet (la dernière version compatible est choisie automatiquement) :

```bash
# Infrastructure
dotnet add src/NyangoPress.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/NyangoPress.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/NyangoPress.Infrastructure package QuestPDF
dotnet add src/NyangoPress.Infrastructure package ClosedXML
dotnet add src/NyangoPress.Infrastructure package QRCoder
dotnet add src/NyangoPress.Infrastructure package MailKit
dotnet add src/NyangoPress.Infrastructure package CsvHelper
dotnet add src/NyangoPress.Infrastructure package Hangfire.Core
dotnet add src/NyangoPress.Infrastructure package Hangfire.PostgreSql
# Application
dotnet add src/NyangoPress.Application package FluentValidation
dotnet add src/NyangoPress.Application package Riok.Mapperly
# Api
dotnet add src/NyangoPress.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/NyangoPress.Api package Hangfire.AspNetCore
dotnet add src/NyangoPress.Api package Serilog.AspNetCore
dotnet add src/NyangoPress.Api package Microsoft.EntityFrameworkCore.Design
# Web
dotnet add src/NyangoPress.Web package MudBlazor
# Tests
dotnet add tests/NyangoPress.UnitTests package FluentAssertions
dotnet add tests/NyangoPress.IntegrationTests package Testcontainers.PostgreSql
```

> **QuestPDF** : la licence *Community* est gratuite sous certaines conditions de chiffre d'affaires. Lisez-la avant une commercialisation et déclarez-la dans le code (`QuestPDF.Settings.License = LicenseType.Community;`).

Vérifier que tout compile :

```bash
dotnet build
```

## Étape 5. Services de développement (Dev 1)

Copiez les fichiers de ce kit dans le dépôt : `docker/`, `.github/`, `docs/`, `assets/`, `README.md`, `CONTRIBUTING.md`, les deux PDF.

```bash
cp docker/.env.example docker/.env      # PowerShell : Copy-Item docker/.env.example docker/.env
# éditez docker/.env et choisissez de vrais mots de passe
echo "docker/.env" >> .gitignore
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d
docker ps                                # trois conteneurs doivent tourner
```

- pgAdmin : http://localhost:5050
- Mailpit (faux serveur d'e-mails) : http://localhost:8025

Secrets de l'API (jamais dans Git) :

```bash
dotnet user-secrets init --project src/NyangoPress.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=nyangopress;Username=nyango;Password=VOTRE_MOT_DE_PASSE" --project src/NyangoPress.Api
dotnet user-secrets set "Jwt:Key" "UNE_LONGUE_CLE_ALEATOIRE_DE_64_CARACTERES_AU_MOINS" --project src/NyangoPress.Api
```

## Étape 6. Premier commit et protection des branches (Dev 1)

Après le squelette de J1 (voir plan d'action) :

```bash
git add .
git commit -m "chore: initialise la solution NyangoPress"
git remote add origin https://github.com/<organisation-ou-compte>/NyangoPress.git
git push -u origin main
git switch -c develop
git push -u origin develop
```

Sur GitHub : *Settings → Branches (ou Rules → Rulesets)*, créez une règle pour `main` **et** `develop` :

- ☑ Require a pull request before merging (**1 approbation**)
- ☑ Require status checks to pass (choisissez le job `build-and-test`, il apparaît après la première exécution de la CI)
- ☑ Require conversation resolution before merging
- ☑ Block force pushes
- ☑ Restrict deletions
- Dans *Settings → General → Pull Requests* : n'autorisez que **Squash merging**, cochez *Automatically delete head branches*.
- Dans *Settings → Collaborators* : vérifiez les droits ; dans `.github/CODEOWNERS`, remplacez `@dev1`...`@dev5`.

## Étape 7. Organiser le travail (Dev 1 + équipe)

1. *Projects → New project → Board* : colonnes **À faire, En cours, En revue, Terminé**.
2. Créez les libellés : `module:auth`, `module:orders`, `module:pay`, `module:ui`, `module:notif`, `P0`, `P1`, `P2`, `J1`...`J7`, `bug`.
3. Créez un ticket par tâche du plan d'action (PDF, chapitres 6 à 10), avec le modèle `Tâche`.
4. Créez les jalons (*Milestones*) : `Fin J1`, `Fin J2`, `Fin P0 (J4)`, `Gel (J6)`, `v1.0 (J7)`.

## Étape 8. Les 4 autres développeurs : premier lancement

```bash
git clone https://github.com/<organisation-ou-compte>/NyangoPress.git
cd NyangoPress
git switch develop

cp docker/.env.example docker/.env     # mots de passe donnés par Dev 1
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d

dotnet user-secrets set "Jwt:Key" "..." --project src/NyangoPress.Api

dotnet restore
dotnet build
dotnet ef database update --project src/NyangoPress.Infrastructure --startup-project src/NyangoPress.Api
dotnet test
dotnet run --project src/NyangoPress.Api     # dans un terminal
dotnet run --project src/NyangoPress.Web     # dans un autre terminal
```

Chaque développeur a **sa propre base locale** pendant le développement (le conteneur est sur sa machine). La base **partagée** du pressing existe seulement en production, sur le PC principal (voir [DEPLOIEMENT_LAN.md](DEPLOIEMENT_LAN.md)).

**Test de vie :** ouvrez la page Swagger de l'API (l'adresse s'affiche au lancement), puis la page d'accueil Web. Si les deux répondent : bravo.

Puis créez votre première branche, faites un petit changement (par exemple ajoutez votre nom dans le README), ouvrez une PR vers `develop` : c'est l'exercice de validation de J1.

## Étape 9. Comptes externes à demander dès le jour 1 (Dev 5, aidé par l'équipe)

Ces validations prennent du temps : ne les repoussez pas. Voir [NOTIFICATIONS.md](NOTIFICATIONS.md).

- [ ] Compte **Meta for Developers** + WhatsApp Business (numéro dédié, modèles de messages à faire approuver)
- [ ] Compte fournisseur **SMS** (Africa's Talking ou Twilio)
- [ ] Un **SMTP** pour les e-mails (Gmail avec mot de passe d'application, Brevo ou autre) ; Mailpit en développement

## Checklist de fin de J1

- [ ] Les 5 développeurs ont cloné, lancé docker, l'API et le Web
- [ ] La CI est verte sur develop
- [ ] Les branches main et develop sont protégées
- [ ] Les interfaces de contrat sont publiées (plan d'action, chapitre 4.3)
- [ ] Le tableau de tickets est rempli pour J2 et J3
- [ ] Chacun a fusionné au moins une PR
