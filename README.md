# Мой Дом.ру · Solid Eagle

[![CI and deploy](https://github.com/Cricko7/unauth-dom-ru/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/Cricko7/unauth-dom-ru/actions/workflows/ci-cd.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Веб-приложение команды **Solid Eagle**, созданное по кейсу «Дом.ру»: проверить соединение, пройти диагностику, посмотреть Wi-Fi рядом и задать вопрос ИИ-ассистенту из одного интерфейса.

**[Открыть демо](http://185.39.206.98)** · [API](docs/API.md) · [Архитектура](docs/ARCHITECTURE.md) · [Запуск и настройки](docs/CONFIGURATION.md) · [Деплой](deploy/README.md) · [CI/CD](docs/CI-CD.md)

## Возможности

- Регистрация и вход по номеру телефона и паролю, авторизация через cookie.
- Измерение download/upload и оценка задержки между браузером и сервером приложения.
- Диагностика подключения с отдельной страницей результата.
- Wi-Fi-раздел с картой Яндекса и демонстрационными карточками точек.
- ИИ-помощник по вопросам интернета, роутеров и Wi-Fi через внешний API.
- Светлая и тёмная темы, адаптивная вёрстка.

Это прототип: SMS-подтверждение, реальное подключение к Wi-Fi-точкам и интеграция с личным кабинетом оператора не реализованы. Для ответов ИИ нужны действующий ключ и квота провайдера. Демо пока работает по HTTP; HTTPS-конфигурация подготовлена для домена.

## Технологии

| Слой | Реализация |
| --- | --- |
| Приложение | C#, .NET 9, ASP.NET Core Razor Pages и API-контроллеры |
| Интерфейс | Razor, HTML/CSS, JavaScript без отдельной Node.js-сборки |
| Авторизация | ASP.NET Core Identity, cookie, antiforgery-токены форм |
| Данные | PostgreSQL 17, EF Core и Npgsql |
| Инфраструктура | Docker, Compose, Caddy, GitHub Actions |

## Быстрый локальный запуск

Нужны Git, Docker Engine/Desktop с Linux-контейнерами и Docker Compose v2 или новее. .NET SDK на хосте для этого варианта не требуется.

```bash
git clone https://github.com/Cricko7/unauth-dom-ru.git
cd unauth-dom-ru
cp .env.example .env
```

В PowerShell вместо последней команды: `Copy-Item .env.example .env`. Заполните `POSTGRES_PASSWORD` в `.env` случайной hex-строкой длиной от 32 символов. `AI_API_KEY` можно оставить пустым, если чат не нужен. `.env` не попадает в Git и Docker build context.

```bash
docker compose up -d --build --wait --wait-timeout 240
```

Откройте **http://localhost:8080**. PostgreSQL и структура таблиц создаются автоматически; исходного дампа данных в репозитории нет.

```bash
curl --fail http://localhost:8080/health
python3 deploy/smoke.py
docker compose logs --tail=100 web db
docker compose down
```

В Windows используйте `curl.exe` и `python`, если так называются установленные команды. Smoke-тест создаёт тестовый аккаунт. Обычный `down` сохраняет данные; `down -v` удаляет тома и нужен только для одноразовых тестовых окружений.

## Другие варианты запуска

| Сценарий | Инструкция |
| --- | --- |
| .NET на хосте и отдельная PostgreSQL | [Локальная разработка](docs/CONFIGURATION.md#запуск-net-на-хосте) |
| VPS, доступ по IP через HTTP | [Развёртывание](deploy/README.md#сервер-по-ip-http) |
| VPS с доменом и HTTPS | [HTTPS](deploy/README.md#домен-и-https) |
| Автоматический деплой после CI | [GitHub Actions](docs/CI-CD.md) |
| Проверки проведённого развёртывания | [Результаты](deploy/VALIDATION.md) |

## Структура репозитория

```text
.
├── DoomRu.sln                  # решение Visual Studio / dotnet
├── DoomRu/                     # основное ASP.NET Core приложение
│   ├── Controllers/            # JSON API: чат и спидтест
│   ├── Data/                   # EF Core / Identity-контекст
│   ├── Models/                 # DTO чата и вспомогательные модели
│   ├── Pages/                  # страницы Razor, Auth и общий layout
│   ├── Services/               # клиент внешнего ИИ API
│   ├── wwwroot/                # CSS, JS, изображения и vendor-библиотеки
│   ├── Properties/             # локальные профили запуска
│   └── Program.cs              # DI, middleware, маршруты и создание БД
├── 123/                        # отдельный исходный HTML-прототип
├── docs/                       # API, архитектура, настройки и CI/CD
├── deploy/                     # deploy-скрипты, Caddy и smoke-тест
├── .github/workflows/          # GitHub Actions
├── Dockerfile                  # многоэтапная сборка приложения
├── compose*.yaml               # БД, приложение, HTTP/HTTPS и dev override
└── .env.example                # шаблон приватных настроек
```

Рабочий интерфейс находится в `DoomRu/Pages` и `DoomRu/wwwroot`. Папка `123` сохраняет HTML-прототип и не публикуется приложением. Потоки запросов и модель хранения описаны в [архитектуре](docs/ARCHITECTURE.md).

## API в одном экране

| Метод | Маршрут | Назначение |
| --- | --- | --- |
| GET | `/health` | Проверка подключения к БД |
| POST | `/api/aichat/send` | Сообщение ассистенту и история диалога |
| GET | `/api/speedtest/download?mb=10` | Случайные байты для измерения download |
| POST | `/api/speedtest/upload` | Приём тела запроса для измерения upload |

Форматы, ограничения, примеры curl и особенности ошибок — в [API.md](docs/API.md). Вход и регистрация работают через Razor-формы, а не JWT API.

## Разработка и вклад

Создайте ветку, внесите изменения и откройте pull request. CI собирает контейнеры и проверяет API, статику, авторизацию и сохранение данных/сессии при пересоздании контейнеров. После успешного CI изменения в `main` автоматически развёртываются, если включён CD и настроены секреты. См. [CI/CD](docs/CI-CD.md).

Секреты, локальные БД, бэкапы, `bin`, `obj` и кэш IDE не коммитятся. Для изменения существующей схемы БД нужны миграции: текущий `EnsureCreatedAsync` создаёт начальную схему, но не обновляет её. Проект распространяется по [MIT License](LICENSE).

## Поддержите проект и свяжитесь с нами

Если проект оказался полезен, **поставьте ⭐ этому репозиторию** — это поможет поддержать работу команды **Solid Eagle**. По вопросам проекта, сотрудничества и обратной связи пишите нам в Telegram:

| Участник команды | Telegram |
| --- | --- |
| Степанов Роман Сергеевич | [@giraffick](https://t.me/giraffick) |
| Илларионова Дарья Александровна | [@Korneshon26](https://t.me/Korneshon26) |
| Гарагуля Илья Дмитриевич | [@iliaDGar](https://t.me/iliaDGar) |
| Белоусов Илья Романович | [@maaichk](https://t.me/maaichk) |
| Холод Полина Анатольевна | [@kslefl](https://t.me/kslefl) |

Будем рады вашим вопросам и идеям — обращайтесь к Solid Eagle!
