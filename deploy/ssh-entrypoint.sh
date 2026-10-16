#!/usr/bin/env bash
# Install as /usr/local/sbin/doomru-ci-entrypoint; reference it in authorized_keys.
set -euo pipefail
export PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
if [[ ${SSH_ORIGINAL_COMMAND:-} =~ ^deploy\ ([0-9a-f]{40})$ ]]; then
    revision=${BASH_REMATCH[1]}
    mode=$(cat /etc/doomru-deploy-mode)
    case "$mode" in http|https) ;; *) echo 'Invalid server deployment mode' >&2; exit 2 ;; esac
    exec bash /opt/unauth-dom-ru/deploy/deploy.sh "$revision" "$mode"
fi
echo 'This key only accepts: deploy <40-character-main-commit-sha>' >&2
exit 2
