# Развёртывание и обслуживание

[← README](../README.md) · [CI/CD](../docs/CI-CD.md) · [Настройки](../docs/CONFIGURATION.md) · [Результаты проверок](VALIDATION.md)

## Подготовка сервера

Проверенный вариант: Ubuntu 22.04 x86_64, около 2 ГБ RAM и 29 ГБ диска. Нужны Git, curl, Python 3, Docker Engine, Compose v2+, Bash и flock. Инструкция установки Docker: [официальный apt-репозиторий](https://docs.docker.com/engine/install/ubuntu/).

```bash
git clone https://github.com/Cricko7/unauth-dom-ru.git /opt/unauth-dom-ru
cd /opt/unauth-dom-ru
cp .env.example .env
chmod 600 .env
```

Заполните `POSTGRES_PASSWORD` случайным hex-паролем (от 32 символов), при необходимости `AI_API_KEY`. PostgreSQL создаётся пустой; существующие данные переносятся отдельным дампом. Пароли и ключи сохраняются на VPS, а не в Git. Для текущего сервера `.env` уже настроен — не перезаписывайте его шаблоном.

## Сервер по IP: HTTP

```bash
docker compose -f compose.yaml -f compose.http.yaml config --quiet
docker compose -f compose.yaml -f compose.http.yaml up -d --build --wait --wait-timeout 240
curl --fail http://127.0.0.1/health
```

Сайт доступен по `http://SERVER_IP`. Разрешите TCP 80 в сетевом экране провайдера. Приложение публикуется на localhost:8080, БД доступна только внутри Docker-сети. HTTP подходит для демонстрации; для передачи настоящих паролей подключите HTTPS.

Если Docker Hub недоступен, добавьте в `.env`:

```dotenv
POSTGRES_IMAGE=public.ecr.aws/docker/library/postgres:17-bookworm
CADDY_IMAGE=public.ecr.aws/docker/library/caddy:2
```

## Домен и HTTPS

Направьте A-запись домена на IP сервера, откройте TCP 80/443 (UDP 443 — для HTTP/3). AAAA-запись используйте только при рабочем IPv6. В `.env` задайте `DOMAIN=example.org`, `ACME_EMAIL=you@example.org`; сохраните `APP_BIND=127.0.0.1`, `APP_PORT=8080`.

```bash
docker compose -f compose.yaml -f compose.production.yaml config --quiet
```

При переходе с HTTP-конфигурации меняется сеть Compose: остановите прежний вариант **без удаления томов**, затем запустите HTTPS. Возникнет краткий простой.

```bash
docker compose -f compose.yaml -f compose.http.yaml down
docker compose -f compose.yaml -f compose.production.yaml up -d --build --wait --wait-timeout 240
curl --fail https://example.org/health
```

Caddy получает и обновляет сертификаты, хранит их в `caddy-data`. Подсеть `172.30.77.0/24` не должна пересекаться с сетями VPS. При её изменении синхронно обновите адрес Caddy и `ReverseProxy__KnownProxy`.

Для настроенного CD переключите режим на VPS: `printf 'https\n' > /etc/doomru-deploy-mode`, а в GitHub обновите `DEPLOY_URL` на HTTPS-origin. Режим переключайте вручную после проверки DNS и сертификата, а не во время обычного автодеплоя.

## Обновление конкретного commit

Для автоматического обновления настройте [CI/CD](../docs/CI-CD.md). Вручную администратор может выполнить:

```bash
cd /opt/unauth-dom-ru
bash deploy/deploy.sh FULL_40_CHARACTER_MAIN_COMMIT_SHA http
# Для домена вместо http укажите https; для localhost — local.
```

Скрипт проверяет чистоту checkout и происхождение SHA из main, создаёт бэкап, сохраняет старый image, собирает новый и ждёт readiness. При неудаче пытается вернуть предыдущие checkout/image; данные и ключи не удаляет. Он оставляет сервер на **detached HEAD**, поэтому последующие обновления выполняйте этим скриптом, а не `git pull`.

Рабочий образ помечается `doomru-web:<sha>`. Для диагностических команд Compose, которым требуется конфигурация образа, используйте временный override, показанный ниже; не запускайте обычный `up --build` на проде вместо deploy-скрипта, если требуется сохранить точный image/tag.

## Проверки и логи

```bash
docker compose -f compose.yaml -f compose.http.yaml ps
docker compose -f compose.yaml -f compose.http.yaml logs --tail=100 web db proxy
curl --fail http://127.0.0.1/health
docker compose exec db psql -U doomru -d doomru -c '\dt'
```

Для HTTPS замените `compose.http.yaml` на `compose.production.yaml`. `restart: unless-stopped` обеспечивает перезапуск при сбое и после рестарта Docker; проверьте `systemctl is-enabled docker`. Контейнер, явно остановленный администратором, может остаться остановленным после перезагрузки.

`python3 deploy/smoke.py --url http://127.0.0.1` проверяет страницы, API, статику, регистрацию и вход. Тест создаёт пользователя — используйте его на тестовой БД. Проверка сохранности сессии:

```bash
python3 deploy/smoke.py --url http://127.0.0.1 --state-file /tmp/doomru-smoke.json
# Пересоздайте контейнеры, сохранив тома.
python3 deploy/smoke.py --url http://127.0.0.1 --state-file /tmp/doomru-smoke.json
```

Файлы состояния и соседний `.cookies` содержат тестовые учётные данные. Удалите их после проверки, не помещайте в Git. ИИ CI/smoke не вызывает: наличие платной квоты проверяется отдельно.

## Бэкапы и восстановление

Deploy-скрипт перед обновлением работающей БД создаёт `pg_dump -Fc` в `/var/backups/doomru` (права каталога 700, файлов 600). Путь можно переопределить `BACKUP_DIR` при ручном запуске. Скопируйте бэкапы за пределы VPS, следите за свободным диском и периодически удаляйте устаревшие копии. Автоматический retention или расписание бэкапов пока не настроены.

Ручной бэкап:

```bash
install -d -m 700 /var/backups/doomru
umask 077
docker compose exec -T db pg_dump -U doomru -d doomru -Fc > /var/backups/doomru/manual.dump
```

Восстановление заменяет данные. Сначала остановите приложение и сохраните текущую БД, затем восстановите выбранный дамп:

```bash
docker compose stop web
docker compose exec -T db pg_restore -U doomru -d doomru --clean --if-exists --no-owner --exit-on-error < /var/backups/doomru/manual.dump
docker compose start web
curl --fail http://127.0.0.1:8080/health
```

Эта операция не является частью автоматического rollback. На БД с активными подключениями или иной схемой может потребоваться отдельный план восстановления. Дамп PostgreSQL не содержит `app-keys`: сохраняйте защищённую копию этого тома отдельно, если нужно восстановить выданные сессии. Не удаляйте `postgres-data` и `app-keys` при обычном обслуживании.

## Ручной rollback приложения

Deploy-скрипт оставляет image `doomru-web:rollback-<previous_sha>`. Если автоматический rollback не помог, администратор сверяет список `docker image ls doomru-web`, выбирает прежний SHA и возвращает checkout. Пример для HTTP:

```bash
git checkout --detach PREVIOUS_FULL_SHA
cat > /tmp/doomru-rollback.yaml <<'EOF'
services:
  web:
    image: ${WEB_IMAGE:?Set WEB_IMAGE}
EOF
WEB_IMAGE=doomru-web:rollback-PREVIOUS_FULL_SHA docker compose \
  -f compose.yaml -f compose.http.yaml -f /tmp/doomru-rollback.yaml \
  up -d --no-build --wait --wait-timeout 180
curl --fail http://127.0.0.1/health
```

Возврат старого приложения не отменяет изменения схемы БД. До внедрения миграций приложение использует `EnsureCreatedAsync`, который только создаёт начальную схему. Не применяйте несовместимые изменения моделей к действующей БД без плана миграции/восстановления.

## Остановка

Используйте тот же набор Compose-файлов, что и при запуске:

```bash
docker compose -f compose.yaml -f compose.http.yaml down
```

Это сохраняет тома. `--volumes` / `-v` удаляет данные, сертификаты и ключи; на действующем сервере для обычной остановки его не используют. Следите за объёмом Docker images, логов и бэкапов; не выполняйте `docker system prune --volumes` без проверки содержимого.
