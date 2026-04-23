using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DOAN.Data;


namespace DOAN.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _uploadFolder;

        public DocumentsController(AppDbContext context)
        {
            _context = context;

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

                var uploaderName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

                var newDocument = new Document
                {
                    FileName = file.FileName,
                    FilePath = filePath,
                    UpLoadedBy = uploaderName,
                    UpLoadDate = DateTime.UtcNow

                };

                _context.Documents.Add(newDocument);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Upload tài liệu thành công",
                    DocumentId = newDocument.Id,
                    FileName = newDocument.FileName
                });

            }

            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server khi upload: {ex.Message}");
            }

        }

    }

}

