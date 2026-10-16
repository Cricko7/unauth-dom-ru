# HTTP API

[← README](../README.md) · [Архитектура](ARCHITECTURE.md)

Примеры используют `http://localhost:8080`. На VPS замените адрес на домен или IP. JSON использует camelCase; Swagger/OpenAPI endpoint не подключён. Контроллеры доступны без входа и JWT. Примеры curl рассчитаны на Bash; в PowerShell используйте `curl.exe` и соответствующее экранирование JSON.

## GET /health

Проверяет PostgreSQL через `CanConnectAsync`.

```bash
curl -i http://localhost:8080/health
```

Успех: `200 application/json`, `{"status":"healthy"}`. Если БД недоступна во время работы — `503`. Endpoint не вызывает ИИ и не проверяет баланс провайдера, наличие всех таблиц или внешнюю карту. При недоступной БД во время старта процесс приложения может ещё не слушать HTTP.

## POST /api/aichat/send

```bash
curl --request POST http://localhost:8080/api/aichat/send \
  --header 'Content-Type: application/json' \
  --data '{"message":"Как улучшить Wi-Fi?","history":[{"role":"user","content":"Связь хуже в дальней комнате"},{"role":"assistant","content":"Где установлен роутер?"}]}'
```

| Поле | Тип | Поведение |
| --- | --- | --- |
| `message` | string | Непустое сообщение, максимум 500 символов |
| `history` | array | Необязательная история; сервер берёт последние 20 элементов |
| `history[].role` | string | Роль сообщения; клиент обычно передаёт `user` / `assistant` |
| `history[].content` | string | Содержимое сообщения |

История передаётся заново с каждым запросом. Отдельной валидации ролей и длины каждого элемента истории нет. Сервис добавляет системную инструкцию и новое сообщение, задаёт `max_tokens=800`, `temperature=0.7`, `top_p=0.9`. Тайм-аут HttpClient — 60 секунд.

Успех (`200`):

```json
{"success":true,"reply":"Попробуйте поставить роутер ближе к центру квартиры…","error":null}
```

Пустое сообщение или более 500 символов (`400`):

```json
{"success":false,"reply":null,"error":"Пустое сообщение"}
```

Некорректный JSON также может вернуть стандартный `400` ASP.NET Core в формате ProblemDetails.

**Текущий контракт ошибки провайдера:** сервис возвращает текст ошибки, контроллер оборачивает его в HTTP 200 и `success:true`. Например, при недостатке квоты:

```json
{"success":true,"reply":"[Ошибка ИИ: PaymentRequired]","error":null}
```

Возможны `[Ошибка: …]` и `[Пустой ответ]`. Поэтому HTTP 200/`success:true` сейчас не гарантируют успешный ответ модели. Исправление контракта будет отдельным изменением API. Endpoint не имеет rate limiting или обязательной авторизации; при включении платного ключа учитывайте публичный доступ.

## GET /api/speedtest/download

```bash
curl --output /dev/null 'http://localhost:8080/api/speedtest/download?mb=10'
```

`mb` — целое число, по умолчанию 10. Ограничивается диапазоном 1–50: `0` превращается в 1, `100` — в 50. Некорректное целое может привести к `400` model binding.

Ответ `200 application/octet-stream` содержит `mb × 1 048 576` случайных байтов и `Cache-Control: no-store`. Размер считается в MiB. Буфер целиком выделяется в памяти сервера. Скорость вычисляет браузер по размеру и времени запроса; сервер не возвращает JSON с Mbps.

## POST /api/speedtest/upload

```bash
head -c 1048576 /dev/urandom | curl --request POST \
  --header 'Content-Type: application/octet-stream' --data-binary @- \
  http://localhost:8080/api/speedtest/upload
```

Тело — произвольные бинарные данные, без multipart/form-data. Сервер читает поток и возвращает `200`:

```json
{"received":1048576}
```

`received` — принятые байты; пустое тело даёт 0. Лимит — **60 000 000 байтов**, не 60 MiB. Превышение лимита Kestrel приводит к `413`; прокси может иметь собственный меньший лимит. Загруженные данные на диск не сохраняются.

## Страницы авторизации

| Метод | Маршрут | Назначение |
| --- | --- | --- |
| GET, POST | `/Auth/Register` | Форма и создание пользователя |
| GET, POST | `/Auth/Login` | Форма и вход |
| POST | `/Auth/Logout` | Выход |

Это Razor Pages с `application/x-www-form-urlencoded`, cookie и `__RequestVerificationToken`. Регистрация принимает `Input.Phone`, `Input.Password`, `Input.ConfirmPassword`; вход — `Input.Phone`, `Input.Password`, `Input.RememberMe`. Сначала получите страницу и antiforgery-токен, затем отправьте форму в той же сессии. Успех ведёт редиректом на главную, ошибка валидации возвращает HTML формы. Телефон: `+7` + 10 цифр; пароль: минимум 6 символов и хотя бы одна строчная буква согласно правилам Identity. SMS не отправляется.

Рабочий пример программной работы с формами — [`deploy/smoke.py`](../deploy/smoke.py).
