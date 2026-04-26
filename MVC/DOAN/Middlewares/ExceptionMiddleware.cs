using System.Net;
using System.Text.Json;

namespace DOAN.Middlerwares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                //cho phép request đi tiếp đến controller
                await _next(context);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(context,ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            //tạo format JSON để trả về cho người dùng
            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Đã có lỗi không mong muốn xảy ra trên Server. Vui lòng thử lại sau.",
                Detailed = exception.Message
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsJsonAsync(jsonResponse);
        }
    }
}