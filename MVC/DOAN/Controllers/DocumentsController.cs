using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DOAN.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;


namespace DOAN.Controllers
{


    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly String _uploadFolder;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public DocumentsController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            //Directory.GetCurrentDirectory() tìm đến link lưu file vật lý
            //Path.Combine nối chuỗi thông minh
            _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        [HttpPost("upload")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadDocumen(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Vui lòng chọn một file hợp lệ");
            }

            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
            {
                return BadRequest("Hệ thống RAG hiện tại chỉ hỗ trợ phân tích file PDF");
            }

            try
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(_uploadFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var uploaderName = User.FindFirstValue(ClaimTypes.Name) ?? "unknow";
                var newDocument = new Document
                {
                    FileName = file.FileName,
                    FilePath = filePath,
                    UpLoadedBy = uploaderName,
                    UpLoadDate = DateTime.UtcNow,
                };

                _context.Documents.Add(newDocument);
                await _context.SaveChangesAsync();

                var aiEngineUrl = _configuration.GetValue<string>("AiEngineUrl");
                var targetEndpoint = $"{aiEngineUrl}/api/ingest";

                using var client = _httpClientFactory.CreateClient();

                // Mở lại file vật lý vừa lưu để chuẩn bị gửi đi
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var requestContent = new MultipartFormDataContent();

                //Đóng gói file
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                requestContent.Add(streamContent, "file", file.FileName);

                //Đóng gói tham số document_id
                var idContent = new StringContent(newDocument.Id.ToString());
                requestContent.Add(idContent, "document_id");

                //Gửi requesst Post sang Python
                var response = await client.PostAsync(targetEndpoint, requestContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        Message = "Upload và nạp dữ liệu cho AI thành công",
                        DocumentId = newDocument.Id,
                        FileName = newDocument.FileName,
                        AiEngineResponse = responseString,
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, $"Upload file lên ổ cứng thành công, nhưng lỗi khi nạp vào AI: {responseString}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocument()
        {
            var document = await _context.Documents.AsNoTracking().ToListAsync();
            return Ok(document);
        }


        [HttpDelete("{id}")] //lấy id từ trong url
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteDocument(int id)
        {

            //1. Tìm trong database
            var document = await _context.Documents.FindAsync(id);

            if (document == null)
            {
                return NotFound($"Không tìm thấy tài liệu với ID = {id}");
            }

            //2. Xóa file vật lý trên ổ cứng
            if (System.IO.File.Exists(document.FilePath))
            {
                System.IO.File.Delete(document.FilePath);
            }

            // 3. Xóa bản ghi trong Database
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Đã xóa thành công tài liệu: {document.FileName}" });
        }

    }

}

