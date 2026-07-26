using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sircip.Server.Auth;
using Sircip.Server.Data;
using Sircip.Shared.Contracts;

namespace Sircip.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SircipDbContext _db;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(SircipDbContext db, JwtTokenService jwtTokenService)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NombreUsuario) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Debe indicar nombre de usuario y contraseña.");
        }

        var usuario = await _db.Usuarios
            .SingleOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            return Unauthorized();
        }

        var (token, expiraUtc) = _jwtTokenService.GenerarToken(usuario);

        return Ok(new LoginResponse
        {
            Token = token,
            NombreUsuario = usuario.NombreUsuario,
            Rol = usuario.Rol,
            ExpiraUtc = expiraUtc
        });
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        return Ok(new
        {
            NombreUsuario = User.FindFirstValue(ClaimTypes.Name),
            Rol = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}
