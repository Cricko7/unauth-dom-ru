# Docker и виртуальный сервер

## Локальная проверка

Нужны Docker Engine/Desktop и Docker Compose v2 или новее. Локальный .NET SDK не требуется: сборка выполняется внутри Docker.

1. Скопируйте `.env.example` в `.env`.
2. Заполните `POSTGRES_PASSWORD` случайной строкой из 48 шестнадцатеричных символов. При необходимости заполните `AI_API_KEY`.
3. Выполните:

```sh
docker compose config --quiet
docker compose up -d --build --wait --wait-timeout 180
docker compose ps
curl --fail http://localhost:8080/health
```

Откройте http://localhost:8080. Проверьте регистрацию и вход. Без `AI_API_KEY` внешний ИИ недоступен.

Автоматическая проверка (Python 3, только стандартная библиотека): `python deploy/smoke.py`. Она проверяет БД, страницы, CSS/картинки, download/upload и регистрацию/вход; создаёт один тестовый аккаунт, поэтому запускайте её на тестовой БД.

Для проверки после пересоздания контейнеров добавьте `--state-file /tmp/doomru-smoke-state.json` и повторите ту же команду после `down` / `up` без `-v`. Скрипт проверит сохранность сессии и входа. Файл состояния и соседний `.cookies` содержат тестовые учётные данные: храните их вне репозитория и удалите после проверки.

Если Docker Hub недоступен, задайте `POSTGRES_IMAGE=public.ecr.aws/docker/library/postgres:17-bookworm` и `CADDY_IMAGE=public.ecr.aws/docker/library/caddy:2` в `.env`.

Остановка с сохранением данных: `docker compose down`. Ключ `-v` удаляет данные и ключи авторизации — для обычной остановки его не используйте.

## Развёртывание на Linux VPS

Для временной проверки по IP без домена используйте `docker compose -f compose.yaml -f compose.http.yaml up -d --build --wait`. Сайт будет доступен на HTTP-порту 80. Для публичного использования авторизации подключите домен и вариант с HTTPS ниже. Одновременно HTTP- и HTTPS-варианты запускать не нужно: оба используют порт 80. При переходе на HTTPS выполните `docker compose -f compose.yaml -f compose.production.yaml up -d --build --wait --remove-orphans`, сохранив `APP_BIND=127.0.0.1` и `APP_PORT=8080`.

Установите Docker Engine и Compose по официальной инструкции для ОС сервера. Направьте A-запись домена на IP сервера (AAAA указывайте только при рабочем IPv6). Откройте TCP 80/443 и SSH в межсетевом экране. Порт PostgreSQL наружу не публикуется.

```sh
git clone https://github.com/Cricko7/unauth-dom-ru.git
cd unauth-dom-ru
cp .env.example .env
chmod 600 .env
# Заполните POSTGRES_PASSWORD, DOMAIN, ACME_EMAIL и при необходимости AI_API_KEY.
docker compose -f compose.yaml -f compose.production.yaml config --quiet
docker compose -f compose.yaml -f compose.production.yaml up -d --build --wait --wait-timeout 180
curl --fail https://YOUR_DOMAIN/health
```

Caddy получает и обновляет TLS-сертификат. Приложение доступно снаружи через Caddy; HTTP-порт приложения привязан к localhost. Подсеть Compose `172.30.77.0/24` не должна пересекаться с сетями сервера. Если меняете IP прокси, также измените `ReverseProxy__KnownProxy`.

## Проверки и обновления

```sh
docker compose logs --tail=100 web db
docker compose exec db psql -U doomru -d doomru -c '\dt'
```

После перезапуска приложения проверьте, что пользователь может войти, а данные сохранены. Ключи ASP.NET Data Protection находятся в отдельном постоянном томе. Доступ к тому следует ограничить: ключи хранятся без дополнительного шифрования.

Перед обновлением сделайте резервную копию (Linux):

```sh
mkdir -p backups
docker compose exec -T db pg_dump -U doomru -d doomru -Fc > backups/doomru.dump
git pull --ff-only
docker compose -f compose.yaml -f compose.production.yaml up -d --build --wait --wait-timeout 180
```

Файлы резервных копий храните вне репозитория и копируйте за пределы сервера. Смена `POSTGRES_PASSWORD` в `.env` не меняет пароль уже созданной БД автоматически.

Схема пока создаётся через `EnsureCreatedAsync`; для последующих изменений схемы потребуются миграции EF Core. Перенос существующих данных из внешней PostgreSQL выполняется отдельно: дампа в исходном проекте нет.

Официальные инструкции: https://docs.docker.com/engine/install/ и https://docs.docker.com/compose/how-tos/startup-order/.
