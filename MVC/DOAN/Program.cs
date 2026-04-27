using DOAN.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext (Kết nối SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Cấu hình Swagger để thêm nút Ổ khóa (Authorize)
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Nhập token theo cú pháp: bearer {token}\nVí dụ: bearer eyJhbGciOiJIUzUxMi...",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();

});

// 3. Cấu hình JWT Authentication (Đọc Secret Key từ appsettings.json)
var tokenSecret = builder.Configuration.GetSection("AppSettings:Token").Value;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecret ?? "")),// dòng này là tạo 1 mã secretkey để khi người dùng đưang nhập sẽ bao gồm data + signatura=HASH(data +secretkey) rồi khi hệ thống nhận sẽ lại mã hóa 1 lần nữa signature2 = HASH(data + secretkey), nếu trên đường vận truyển mà phần data bị thay đổi mà vẫn secretkey không thể thay đổi được thì 2 cái sig vưới sig2 khác nhau
            ValidateIssuer = false,// Tạm tắt cho môi trường Dev
            ValidateAudience = false// Tạm tắt cho môi trường Dev
        };

    });

builder.Services.AddControllersWithViews();


// Cấu hình CORS để cho phép Frontend và Python gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin() //cho phep moi ten mien, cong
                .AllowAnyMethod() //cho phep moi phuong thuc (get,post,put,delete)
                .AllowAnyHeader(); // cho phep moi header
    });
});


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 4. Cấu hình Pipeline (Thứ tự ở đây RẤT QUAN TRỌNG)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

// Kích hoạt cổng CORS
app.UseCors("AllowAll");

// Bắt buộc: Authentication (Xác thực ai là ai) phải nằm TRƯỚC Authorization (Phân quyền làm gì)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();