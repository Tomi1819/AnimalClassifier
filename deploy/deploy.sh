#!/usr/bin/env bash
#
# Deploys one commit of the API or the website, whose images the repository's CD
# workflow has already published:
#
#   sudo animalclassifier-deploy api <full commit hash from the backend's master>
#   sudo animalclassifier-deploy web <full commit hash from the frontend's main>
#
# Installed as root, to /usr/local/sbin/animalclassifier-deploy, so that whoever
# may run it cannot change what it does. It works in /srv/animalclassifier, which
# holds compose.yaml and the .env file with the secrets and the two tags.
#
# What it prints may end up in a public workflow log, so it never prints the
# settings, nor what the containers log, which can hold users' addresses.

set -euo pipefail

readonly DEPLOY_DIR=/srv/animalclassifier
readonly COMPOSE_URL=https://raw.githubusercontent.com/Tomi1819/AnimalClassifier
readonly SITE_URL=http://127.0.0.1:8080/
readonly HEALTH_URL=http://127.0.0.1:8080/api/health
readonly LOCK_FILE=/run/lock/animalclassifier-deploy.lock

# How long a new version has to answer as healthy: the API loads the model first.
readonly HEALTH_ATTEMPTS=30
readonly HEALTH_INTERVAL_SECONDS=2

# Where the database backups taken before a deployment go, inside the database's
# container, and how many are kept.
readonly BACKUP_DIR=/var/opt/mssql/backup
readonly BACKUPS_KEPT=5

log() {
    printf '==> %s\n' "$*"
}

fail() {
    printf 'Error: %s\n' "$*" >&2
    exit 1
}

usage() {
    printf 'Usage: %s api|web <full commit hash>\n' "${0##*/}" >&2
    exit 2
}

# Both the website and the API behind it, the way a visitor reaches them.
healthy() {
    local attempt
    for ((attempt = 1; attempt <= HEALTH_ATTEMPTS; attempt++)); do
        if curl --fail --silent --output /dev/null --max-time 5 "$SITE_URL" \
            && curl --fail --silent --output /dev/null --max-time 5 "$HEALTH_URL"; then
            return 0
        fi
        sleep "$HEALTH_INTERVAL_SECONDS"
    done
    return 1
}

# The files the last deployment left are put back and started again. Migrations
# already run stay, which is why they only ever add to the schema, and why the
# database was backed up before them.
roll_back() {
    log "The new version is not healthy, so the previous one goes back."
    cp -p .env.previous .env
    cp -p compose.yaml.previous compose.yaml

    if docker compose up --detach --quiet-pull && healthy; then
        log "The previous version is back and healthy."
    else
        log "The previous version is not healthy either."
    fi
    fail "Deploying $component $commit failed. See why with: sudo docker compose -f $DEPLOY_DIR/compose.yaml logs $component"
}

# Taken inside the database's container, onto its own volume, so it is only a
# guard against a migration gone wrong, not a backup of the machine.
back_up_database() {
    local name
    name="pre-deploy-$(date --utc +%Y%m%dT%H%M%SZ)-${commit:0:7}.bak"
    log "Backing up the database to $name."

    # The password is the container's own, so its \$ is left for the
    # container's shell; everything else is filled in here.
    docker compose exec -T sqlserver sh -c "
        set -e
        mkdir -p $BACKUP_DIR
        SQLCMDPASSWORD=\"\$MSSQL_SA_PASSWORD\" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b \
            -Q \"BACKUP DATABASE [AnimalClassifier] TO DISK = N'$BACKUP_DIR/$name' WITH INIT, CHECKSUM\"
        ls -1t $BACKUP_DIR/pre-deploy-*.bak | tail -n +$((BACKUPS_KEPT + 1)) | xargs -r rm --"
}

# Every image of what was deployed except this version and the one before it,
# which a rollback by hand may want back.
remove_old_images() {
    local repository tag
    for repository in "$@"; do
        docker image ls "$repository" --format '{{.Tag}}' | while read -r tag; do
            if [[ $tag != "$commit" && $tag != "$previous_commit" ]]; then
                docker image rm "$repository:$tag" > /dev/null || true
            fi
        done
    done
}

[[ $# -eq 2 ]] || usage
component=$1
commit=$2

case $component in
    api)
        tag_variable=API_TAG
        services=(api migrations)
        ;;
    web)
        tag_variable=WEB_TAG
        services=(web)
        ;;
    *) usage ;;
esac

[[ $commit =~ ^[0-9a-f]{40}$ ]] || usage

cd "$DEPLOY_DIR"

# One deployment at a time, as the API's and the website's can both be asked for
# at once, and each rewrites the same files.
exec 9> "$LOCK_FILE"
flock --wait 900 9 || fail "Another deployment has been running for 15 minutes."

trap 'rm -f .env.next compose.yaml.next' EXIT

previous_commit=$(sed -n "s/^$tag_variable=//p" .env)
log "Deploying $component $commit (running now: ${previous_commit:-nothing})."

# The settings for the new version are prepared beside the running ones, so that
# nothing running changes until the new version is ready to start. The compose
# file comes from the API's own commit, as the two are changed together; the
# website's deployment keeps the one that is running.
if [[ $component == api ]]; then
    curl --fail --silent --show-error --proto '=https' --retry 3 \
        --output compose.yaml.next "$COMPOSE_URL/$commit/compose.yaml"
else
    cp -p compose.yaml compose.yaml.next
fi

sed "s/^$tag_variable=.*/$tag_variable=$commit/" .env > .env.next
chmod 600 .env.next
grep --quiet --line-regexp "$tag_variable=$commit" .env.next \
    || fail ".env has no $tag_variable line to change."

# Checks that every setting the new compose file asks for is in .env, without
# printing them.
docker compose --file compose.yaml.next --env-file .env.next config --quiet \
    || fail "The new compose file does not work with this machine's .env."

log "Pulling the images."
docker compose --file compose.yaml.next --env-file .env.next pull --quiet "${services[@]}"

if [[ $component == api ]]; then
    back_up_database

    log "Running the migrations."
    docker compose --file compose.yaml.next --env-file .env.next run --rm --quiet-pull migrations
fi

log "Starting the new version."
cp -p .env .env.previous
cp -p compose.yaml compose.yaml.previous
mv .env.next .env
mv compose.yaml.next compose.yaml

if ! { docker compose up --detach --quiet-pull && healthy; }; then
    roll_back
fi

if [[ $component == api ]]; then
    remove_old_images ghcr.io/tomi1819/animalclassifier-api ghcr.io/tomi1819/animalclassifier-migrations
else
    remove_old_images ghcr.io/tomi1819/animalclassifier-web
fi

log "Deployed $component $commit."
