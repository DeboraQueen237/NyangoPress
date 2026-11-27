# Installation sur le PC principal du pressing

Objectif : un PC (le **serveur**) héberge la base et l'application ; tous les autres appareils du pressing s'y connectent par le navigateur. Toutes les données sont ainsi partagées en temps réel.

## 1. Le PC serveur
- Windows 11 ou Linux, 16 Go de RAM recommandés, SSD.
- **IP fixe** sur le réseau local (réservation DHCP dans le routeur) : ex. `192.168.1.10`.
- Branché sur **onduleur** ; jamais en veille.
- Pare-feu : autoriser les ports de l'application (ex. 443) pour le réseau local **seulement** ; ne pas exposer PostgreSQL (5432) hors du serveur.

## 2. Installation
1. Installer Docker Desktop (ou Docker Engine sous Linux) et le .NET 10 Runtime/SDK.
2. Récupérer le projet : `git clone` puis `git checkout main` (version étiquetée).
3. Créer `docker/.env` avec des **mots de passe forts**.
4. Démarrer la base : `docker compose -f docker/docker-compose.yml --env-file docker/.env up -d db`
5. Publier : `dotnet publish src/NyangoPress.Api -c Release -o /opt/nyangopress/api` et `dotnet publish src/NyangoPress.Web -c Release -o /opt/nyangopress/web`
6. Variables d'environnement du serveur : chaîne de connexion, clé JWT, clés WhatsApp, SMS, SMTP.
7. Appliquer les migrations : `dotnet ef database update` (ou le bundle de migrations).
8. Faire démarrer API et Web **automatiquement** : service Windows (`sc create` / NSSM) ou `systemd` sous Linux.

## 3. HTTPS en local
Utiliser **mkcert** : `mkcert -install` puis `mkcert nyangopress.local 192.168.1.10`. Installer le certificat racine sur les appareils du personnel. Déclarer `nyangopress.local` dans le routeur (DNS local) ou dans les fichiers *hosts*.

## 4. Postes du personnel
Ouvrir `https://nyangopress.local` dans Chrome ou Edge → *Installer l'application* (PWA). Aucun logiciel à installer.

## 5. Portail de suivi pour les clients (hors réseau local)
Seules les routes publiques (`/suivi/...`) doivent être joignables depuis internet.

- Solution recommandée : **Cloudflare Tunnel** (gratuit) qui expose un nom de domaine public vers le serveur, sans ouvrir de port sur le routeur.
- Règles : n'exposer que le préfixe `/suivi` et `/hubs/tracking` ; limitation de débit ; aucune donnée sensible dans ces pages.
- Les liens envoyés dans les messages (`{{tracking_url}}`) utilisent ce domaine public.

## 6. Sauvegardes
- Tâche planifiée quotidienne : `pg_dump` compressé, conservé 30 jours.
- **Copie hors du serveur** (disque externe ou stockage cloud chiffré) : un seul PC qui tombe en panne ne doit pas faire perdre l'entreprise.
- **Testez la restauration** au moins une fois avant la mise en service.

## 7. Si internet tombe
L'application continue en local. Les notifications attendent dans la file et partent au retour du réseau. Le suivi public et le suivi GPS du livreur dépendent d'internet.

## 8. Mises à jour
`git pull` sur `main` → sauvegarde → publication → migrations → redémarrage des services → test de connexion et d'un dépôt.
