# unauth-dom-ru
Решение от команды Solid Eagle по кейсу от "Дом.ру"

## Локальная настройка

Проект использует .NET 9 и PostgreSQL. В папке `123` находится отдельная HTML-верстка, в `DoomRu` — ASP.NET Core приложение.

Секреты передаются через переменные окружения (PowerShell):

```powershell
$env:ConnectionStrings__DoomRuContext = "Host=localhost;Port=7777;Database=DoomRu;Username=postgres;Password=<ваш пароль>"
$env:ApiSettings__ApiKey = "<ваш API-ключ>"
dotnet run --project DoomRu/DoomRu.csproj
```

PostgreSQL должен быть запущен. Приложение создаёт структуру базы при первом запуске через `EnsureCreatedAsync`. Дамп данных в исходной папке отсутствует.

Кэш Visual Studio, результаты сборки и локальные секреты исключены из Git.
