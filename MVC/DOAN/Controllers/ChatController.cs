using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOAN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public class ChatMessageDto
        {
            public string Role { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;

        }
        public class ChatRequestDto
        {
            public string Query { get; set; } = string.Empty;
            public string? document_id { get; set; }
            public List<ChatMessageDto> History { get; set; } = new List<ChatMessageDto>();
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskAi([FromBody] ChatRequestDto request)
        {
            try
            {
                var aiEngineUrl = _configuration.GetValue<string>("AiEngineUrl");
                var targetEndpoint = $"{aiEngineUrl}/api/chat";

                //Chuẩn bị dữ liệu gửi sang Python
                var payload = new
                {
                    query = request.Query,
                    document_id = request.document_id,
                    top_k = 3,
                    history = request.History.Select(h => new
                    {
                        role = h.Role.ToLower(),
                        content = h.Content,
                    }).ToList()
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                //Gọi sang python
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(targetEndpoint, content);

                //Đọc kết quả trả về
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var resultData = JsonSerializer.Deserialize<object>(responseString);
                    return Ok(resultData);
                }
                else
                {
                    return StatusCode((int)response.StatusCode, $"Lỗi từ AI Engine: {responseString}");
                }
            }

            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server .NET: {ex.Message}");
            }
        }

    }
}