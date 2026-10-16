#!/usr/bin/env bash
# Deploy an immutable commit. Keep database volumes and private .env on the server.
set -Eeuo pipefail

main() {
    local revision=${1:?Usage: deploy.sh COMMIT_SHA [local|http|https]}
    local mode=${2:-http}
    [[ $revision =~ ^[0-9a-f]{40}$ ]] || { echo 'Expected a full 40-character commit SHA' >&2; return 2; }
    case "$mode" in local|http|https) ;; *) echo 'Unknown deployment mode' >&2; return 2 ;; esac
    local repo
    repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
    cd "$repo"
    umask 077
    exec 9> .git/doomru-deploy.lock
    flock -w 300 9
    [[ -z $(git status --porcelain) ]] || { echo 'Checkout has local changes; refusing to overwrite them' >&2; return 1; }
    [[ -f .env ]] || { echo 'Configure private .env before deployment' >&2; return 1; }
    git fetch origin main
    git cat-file -e "$revision^{commit}"
    git merge-base --is-ancestor "$revision" origin/main || { echo 'Commit must belong to origin/main' >&2; return 1; }

    local previous override image_id='' db_id='' rollback_image=''
    previous=$(git rev-parse HEAD)
    override=$(mktemp /tmp/doomru-image.XXXXXX.yaml)
    cat > "$override" <<'YAML'
services:
  web:
    image: "${WEB_IMAGE:?Set WEB_IMAGE}"
YAML
    local -a files=(-f compose.yaml)
    case "$mode" in
        http) files+=(-f compose.http.yaml) ;;
        https) files+=(-f compose.production.yaml) ;;
    esac
    files+=(-f "$override")
    export WEB_IMAGE="doomru-web:$revision"
    local web_id
    web_id=$(docker compose "${files[@]}" ps --all -q web)
    db_id=$(docker compose "${files[@]}" ps -q db)
    if [[ -n $web_id ]]; then
        image_id=$(docker inspect --format '{{.Image}}' "$web_id")
        rollback_image="doomru-web:rollback-$previous"
        docker image tag "$image_id" "$rollback_image"
    fi

    rollback() {
        local status=$?
        trap - ERR
        set +e
        echo "Deployment failed (exit $status); restoring application commit $previous" >&2
        docker compose "${files[@]}" logs --no-color --tail=60 web >&2
        git checkout --detach "$previous"
        if [[ -n $rollback_image ]]; then
            export WEB_IMAGE="$rollback_image"
            docker compose "${files[@]}" up -d --no-build --wait --wait-timeout 180
            echo 'Rollback attempted; database was not restored or deleted' >&2
        else
            echo 'No previous application image exists; manual recovery is required' >&2
        fi
        rm -f "$override"
        exit "$status"
    }
    trap rollback ERR

    if [[ -n $db_id ]]; then
        local backup_dir=${BACKUP_DIR:-/var/backups/doomru}
        local backup
        install -d -m 700 "$backup_dir"
        backup="$backup_dir/$(date -u +%Y%m%dT%H%M%SZ)-${previous:0:12}-$$.dump"
        docker exec "$db_id" pg_dump -U doomru -d doomru -Fc > "$backup.partial"
        mv "$backup.partial" "$backup"
        echo "Database backup: $backup"
    fi
    git checkout --detach "$revision"
    docker compose "${files[@]}" config --quiet
    docker compose "${files[@]}" build web
    docker compose "${files[@]}" up -d --no-build --wait --wait-timeout 180
    docker compose "${files[@]}" exec -T web curl --fail --silent http://localhost:8080/health
    trap - ERR
    rm -f "$override"
    printf '\nDeployed %s (%s)\n' "$revision" "$mode"
}

main "$@"
