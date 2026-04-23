
using DOAN.Data;
using DOAN.Models;
using DOAN.Models.Entites;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace DOAN.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public AuthController(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;

    }

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(UserDto request)
    {
        //1. Tạo Hash và salt mật khẩu
        using var hmac = new System.Security.Cryptography.HMACSHA3_512();
        var user = new User
        {
            Username = request.Username,
            PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password)),
            PasswordSalt = hmac.Key,
            Role = "Staff"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(UserDto request)
    {
        //1. Kiểm tra tên tài khoản
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
            return BadRequest("User không tồn tại");

        //2. kiểm tra mật khẩu
        using var hmac = new System.Security.Cryptography.HMACSHA3_512(user.PasswordSalt);
        var computeHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password));

        if (!computeHash.SequenceEqual(user.PasswordHash))
            return BadRequest("Sai mật khẩu");

        //3. Cấp thẻ thông hành
        string token = CreateToken(user);
        return Ok(token);
    }

    private string CreateToken(User user)
    {
        // 1. Khai báo các thông tin (Claims) sẽ nhét vào Token
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var tokenSecret = _configuration.GetSection("Appsettings:Token").Value;
        if (string.IsNullOrEmpty(tokenSecret))
        {
            throw new Exception("Chưa cấu hình AppSettings:Token trong appsettings.json");
        }

        //3.tạo chữ kí điện tử
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(tokenSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature); 
        
        
        //4. Đóng gói token(thời hạn 1 ngày)
        var token = new JwtSecurityToken(
            claims:claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds
        );

        // 5. Xuất ra chuỗi string
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return jwt;
    }





}