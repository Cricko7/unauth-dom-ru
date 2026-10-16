using DoomRu.Models;
using DoomRu.Services;
using Microsoft.AspNetCore.Mvc;

namespace DoomRu.Controllers
{
    /// <summary>
    /// REST-эндпоинт для чата с ИИ.
    /// Вызывается из JS на главной странице.
    /// История хранится ТОЛЬКО на клиенте и не пишется в БД —
    /// при F5 всё слетает.
    /// </summary>
    [ApiController]
    [Route("api/aichat")]
    public class AiChatController : ControllerBase
    {
        private const int MaxPromptLength = 500;

        private readonly AiChatService _ai;

        public AiChatController(AiChatService ai)
        {
            _ai = ai;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ChatRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new ChatResponseDto { Success = false, Error = "Пустое сообщение" });

            // Ограничение по длине промта — 500 символов
            if (dto.Message.Length > MaxPromptLength)
                return BadRequest(new ChatResponseDto
                {
                    Success = false,
                    Error = $"Сообщение не должно превышать {MaxPromptLength} символов"
                });

            // Защита от слишком длинной истории (иначе токенов не хватит)
            var history = dto.History?.TakeLast(20).ToList() ?? new List<ChatMessage>();
            history.Add(new ChatMessage { Role = "user", Content = dto.Message });

            var reply = await _ai.AskAsync(history);

            return Ok(new ChatResponseDto { Success = true, Reply = reply });
        }
    }
}