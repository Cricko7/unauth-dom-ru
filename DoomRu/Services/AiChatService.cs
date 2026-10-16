using System.Text;
using DoomRu.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DoomRu.Services
{
    /// <summary>
    /// Сервис общения с внешним API нейросети.
    /// Логика полностью повторяет ваш старый пример с Llama 3,
    /// но без отправки писем и БД.
    /// </summary>
    public class AiChatService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<AiChatService> _logger;

        public AiChatService(
            IHttpClientFactory httpFactory,
            IConfiguration config,
            ILogger<AiChatService> logger)
        {
            _http = httpFactory.CreateClient();
            _http.Timeout = TimeSpan.FromSeconds(60);
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Отправка истории сообщений в ИИ и получение ответа.
        /// </summary>
        public async Task<string> AskAsync(List<ChatMessage> history)
        {
            var apiKey = _config["ApiSettings:ApiKey"] ?? "";
            var apiUrl = _config["ApiSettings:ApiUrl"] ?? "";
            var model = _config["ApiSettings:Model"] ?? "";
            var sysPrompt = _config["ApiSettings:SystemPrompt"] ?? "Ты вежливый ассистент.";

            // Первым делом — системный промт
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "system", Content = sysPrompt }
            };
            messages.AddRange(history);

            var payload = new
            {
                model = model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                max_tokens = 800,
                temperature = 0.7,
                top_p = 0.9
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            req.Headers.Add("Authorization", $"Bearer {apiKey}");
            req.Content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("AI API error: {code} {body}", resp.StatusCode, body);
                    return $"[Ошибка ИИ: {resp.StatusCode}]";
                }

                var json = JObject.Parse(body);
                var content = json["choices"]?[0]?["message"]?["content"]?.ToString();
                return string.IsNullOrWhiteSpace(content) ? "[Пустой ответ]" : content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI request failed");
                return $"[Ошибка: {ex.Message}]";
            }
        }
    }
}