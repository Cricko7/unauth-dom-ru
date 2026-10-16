using Microsoft.AspNetCore.Mvc;

namespace DoomRu.Controllers
{
    /// <summary>
    /// Технические эндпоинты для клиентского спидтеста
    /// (когда внешний API не подключён).
    /// </summary>
    [ApiController]
    [Route("api/speedtest")]
    public class SpeedTestController : ControllerBase
    {
        private readonly IConfiguration _config;

        public SpeedTestController(IConfiguration config) => _config = config;

        /// <summary>
        /// Скачивание тестового файла заданного размера (по умолчанию 10 МБ).
        /// </summary>
        [HttpGet("download")]
        public IActionResult Download([FromQuery] int mb = 10)
        {
            if (mb < 1) mb = 1;
            if (mb > 50) mb = 50;

            int totalBytes = mb * 1024 * 1024;
            var buffer = new byte[totalBytes];
            // Заполняем случайными данными, чтобы gzip не сжимал
            Random.Shared.NextBytes(buffer);

            Response.Headers["Cache-Control"] = "no-store";
            return File(buffer, "application/octet-stream");
        }

        /// <summary>
        /// Приём пакета данных для измерения upload-скорости.
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(60_000_000)] // 60 МБ
        public async Task<IActionResult> Upload()
        {
            long total = 0;
            var buf = new byte[81920];
            int read;
            while ((read = await Request.Body.ReadAsync(buf)) > 0)
                total += read;

            return Ok(new { received = total });
        }
    }
}
