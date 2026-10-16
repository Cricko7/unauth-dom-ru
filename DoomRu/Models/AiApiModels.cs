using Newtonsoft.Json;

namespace DoomRu.Models
{
    /// <summary>Одно сообщение в чате (роль + текст).</summary>
    public class ChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; } = "user"; // "system" | "user" | "assistant"

        [JsonProperty("content")]
        public string Content { get; set; } = "";
    }

    /// <summary>Запрос, приходящий от клиента на наш API.</summary>
    public class ChatRequestDto
    {
        public string Message { get; set; } = "";
        public List<ChatMessage> History { get; set; } = new();
    }

    /// <summary>Ответ нашего API клиенту.</summary>
    public class ChatResponseDto
    {
        public bool Success { get; set; }
        public string? Reply { get; set; }
        public string? Error { get; set; }
    }
}