# Проверки от 16 октября 2026

Выполнено:

- `docker compose config --quiet` — успешно.
- Объединённая конфигурация `compose.yaml` + `compose.production.yaml` — успешно проверена с тестовыми DOMAIN/ACME_EMAIL.
- Release-публикация через .NET SDK 9.0.319 — успешно, после исправлений без предупреждений.
- Генерация PostgreSQL-схемы EF Core без подключения к БД — присутствуют таблицы Identity пользователей, ролей и токенов.
- Все 93 изображения включены в результат публикации; SHA-256 совпадают с исходными файлами.
- Синтаксис `deploy/smoke.py` — проверен.

Не выполнено:

- Сборка Docker-образа и запуск Compose: Docker Desktop установлен, но Linux Engine недоступен (`dockerDesktopLinuxEngine` отсутствует).
- HTTP-проверки, регистрация/вход с реальной PostgreSQL, перезапуск с сохранением данных и TLS-проверка на VPS.
- Внешний AI API: ключ не предоставлен.

SDK установлен локально в рабочую папку. Для локальной проверки зависимости загружались из официального публичного канала пакетов Microsoft через временный адаптер HTTPS: системный TLS Windows возвращал SEC_E_NO_CREDENTIALS. Проверка уязвимостей NuGet при этом не выполнялась. Dockerfile использует стандартное восстановление из NuGet в Linux, его ещё необходимо проверить запуском.

Следующий шаг после запуска Docker Desktop:

```sh
docker compose up -d --build --wait --wait-timeout 180
python deploy/smoke.py
docker compose restart web
curl --fail http://localhost:8080/health
```

Затем нужно проверить повторный вход ранее созданного пользователя и сохранность данных после `docker compose down` / `up -d`. Порядок развёртывания VPS описан в `deploy/README.md`.
