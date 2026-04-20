
using DOAN.Data;
using DOAN.Models;
using DOAN.Models.Entites;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            Role = "admin"
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
        // Logic tạo JWT dùng thư viện Microsoft.IdentityModel.Tokens
        // (Tôi sẽ gửi chi tiết hàm này khi bạn bắt tay vào code)
        return "JWT_TOKEN_CỦA_BẠN";
    }





}