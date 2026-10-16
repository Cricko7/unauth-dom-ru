# Запуск и конфигурация

[← README](../README.md) · [Деплой](../deploy/README.md)

## Docker Compose

Создайте `.env` из `.env.example`, заполните пароль и выполните `docker compose up -d --build --wait --wait-timeout 240`. Откройте http://localhost:8080. Пароль можно сгенерировать командой `python3 -c "import secrets; print(secrets.token_hex(24))"`.

| Переменная `.env` | Значение / назначение |
| --- | --- |
| `POSTGRES_PASSWORD` | Обязательный случайный hex-пароль БД, минимум 32 символа |
| `AI_API_KEY` | Необязательный ключ ИИ-провайдера |
| `APP_BIND` | Адрес публикации приложения, по умолчанию `127.0.0.1` |
| `APP_PORT` | Порт приложения на хосте, по умолчанию `8080` |
| `DOMAIN` | Только для HTTPS: домен с DNS на сервер |
| `ACME_EMAIL` | Только для HTTPS: контакт сертификатов Caddy |
| `POSTGRES_IMAGE` | По умолчанию `postgres:17-bookworm` |
| `CADDY_IMAGE` | По умолчанию `caddy:2` |

При недоступности Docker Hub доступны значения `public.ecr.aws/docker/library/postgres:17-bookworm` и `public.ecr.aws/docker/library/caddy:2`. Для HTTP/HTTPS на VPS сохраняйте `APP_BIND=127.0.0.1`, `APP_PORT=8080`: наружу публикуется Caddy.

В Compose имя/пользователь БД — `doomru`, адрес для приложения — `db:5432`. Смена `POSTGRES_PASSWORD` в `.env` не меняет пароль существующей PostgreSQL автоматически: нужно согласованно изменить пароль роли в БД. `.env` читается Compose для подстановок и не загружается автоматически при `dotnet run`.

## Запуск .NET на хосте

Нужен **.NET 9 SDK**. PostgreSQL можно установить отдельно или запустить только сервис БД:

```bash
docker compose -f compose.yaml -f compose.development.yaml up -d db --wait
```

Dev override публикует PostgreSQL только на `127.0.0.1:5432`. Не подключайте этот файл на VPS. Если порт занят, измените его в override и строке подключения.

Linux/macOS, Bash:

```bash
export ConnectionStrings__DoomRuContext='Host=localhost;Port=5432;Database=doomru;Username=doomru;Password=YOUR_HEX_PASSWORD'
export HttpsRedirection__Enabled=false
# При необходимости: export ApiSettings__ApiKey='YOUR_API_KEY'
dotnet restore DoomRu.sln
dotnet run --project DoomRu/DoomRu.csproj --launch-profile http
```

Windows, PowerShell:

```powershell
$env:ConnectionStrings__DoomRuContext = 'Host=localhost;Port=5432;Database=doomru;Username=doomru;Password=YOUR_HEX_PASSWORD'
$env:HttpsRedirection__Enabled = 'false'
# При необходимости: $env:ApiSettings__ApiKey = 'YOUR_API_KEY'
dotnet restore DoomRu.sln
dotnet run --project DoomRu/DoomRu.csproj --launch-profile http
```

Используйте тот же пароль, который записан в `.env` БД. Откройте **http://localhost:5112** — это порт профиля `http`. Для проверки: `python3 deploy/smoke.py --url http://localhost:5112` (в Windows — `python`). В Visual Studio откройте `DoomRu.sln`, настройте те же переменные среды и выберите профиль `http`.

Публикация без Docker:

```bash
dotnet publish DoomRu/DoomRu.csproj -c Release -o publish /p:UseAppHost=false
```

Для запуска опубликованного `DoomRu.dll` нужен ASP.NET Core Runtime 9 и настроенная строка подключения. Каталог `publish` — результат сборки; не коммитьте его.

## Настройки ASP.NET Core

Переменные среды переопределяют `appsettings.json`; `__` разделяет вложенные ключи.

| Переменная приложения | Назначение |
| --- | --- |
| `ConnectionStrings__DoomRuContext` | Полная строка подключения PostgreSQL |
| `ApiSettings__ApiKey` | Секрет внешнего ИИ API |
| `ApiSettings__ApiUrl` | OpenAI-совместимый endpoint chat completions |
| `ApiSettings__Model` | Идентификатор модели |
| `ApiSettings__SystemPrompt` | Системная инструкция ассистента |
| `HttpsRedirection__Enabled` | Включение HTTPS-редиректа; базовый Compose отключает |
| `DataProtection__KeysPath` | Постоянный каталог ключей cookie; Compose задаёт `/app/keys` |
| `ReverseProxy__KnownProxy` | Доверенный IP HTTPS-прокси |
| `ASPNETCORE_ENVIRONMENT` | Compose использует `Production` |

Compose передаёт `AI_API_KEY` как `ApiSettings__ApiKey`; остальные настройки ИИ по умолчанию берутся из `appsettings.json`. Чтобы менять их в Docker, добавьте нужные переменные в `web.environment` или отдельный override. Добавление произвольной переменной только в `.env` не передаст её в контейнер автоматически.

Текущий провайдер: `https://api.intelligence.io.solutions/api/v1/chat/completions`, модель: `deepseek-ai/DeepSeek-R1-0528`. Их доступность зависит от провайдера. Секция `SpeedTest` в конфигурации не управляет текущим JS-измерением: оно использует API этого приложения; внешний endpoint на странице пока пуст.

## Частые проблемы

| Симптом | Что проверить |
| --- | --- |
| Docker daemon недоступен | Запущен ли Docker Desktop/Engine; выбран ли Linux engine |
| Ошибка при старте приложения | Готовность PostgreSQL, пароль, адрес `db` внутри Compose |
| На хосте нет подключения к БД | Используется ли `compose.development.yaml`; не занят ли порт 5432 |
| ИИ возвращает `PaymentRequired` | Баланс/квота ключа у провайдера; контракт ошибки описан в API.md |
| После рестарта разлогинивает | Подключён ли прежний том `app-keys`, не использовался ли `down -v` |
| HTTPS не выпускает сертификат | DNS A/AAAA, доступность 80/443, DOMAIN и ACME_EMAIL |
| Таблицы не обновились | Создана ли новая EF-миграция; для старой схемы проверен ли baseline |

Реальные ключи не записывайте в `appsettings.json`. На сервере задайте `.env` права `600` и сохраняйте его вне истории Git.
