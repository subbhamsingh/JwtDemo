
using JwtDemo.Data;
using JwtDemo.Dto;
using JwtDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    //private readonly string _key = "ThisIsMySuperSecretKey12345";
    private readonly string _key;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _config;
    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
        _key = config["Jwt:Key"];
        _passwordHasher = new PasswordHasher<User>();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new User
        {
            Username = request.Username,
            //Password = request.Password,
            Password = _passwordHasher.HashPassword(null, request.Password),
            Role = request.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
       
        var user = await _db.Users
    .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
            return Unauthorized();

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized();


        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
             issuer: _config["Jwt:Issuer"],  
             audience: _config["Jwt:Audience"], 
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
                SecurityAlgorithms.HmacSha256)
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = Guid.NewGuid().ToString();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.Now.AddDays(7);

        await _db.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            AccessToken = jwt,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.Now)
            return Unauthorized();

        var claims = new[]
        {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role)
    };

        var token = new JwtSecurityToken(
             issuer: _config["Jwt:Issuer"],    
             audience: _config["Jwt:Audience"],  
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
                SecurityAlgorithms.HmacSha256)
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse
        {
            AccessToken = jwt,
            RefreshToken = request.RefreshToken
        });
    }
}