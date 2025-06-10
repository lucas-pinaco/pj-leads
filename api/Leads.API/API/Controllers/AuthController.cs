using Leads.API.Domain.Entities;
using Leads.API.Domain.POCO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Leads.API.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            AppDbContext context,
            IPasswordHasher<Usuario> passwordHasher,
            IOptions<JwtSettings> jwtOptions)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtSettings = jwtOptions.Value;
        }

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Senha { get; set; }
        }

        public class RegisterRequest
        {
            public string NomeUsuario { get; set; }
            public string Email { get; set; }
            public string Senha { get; set; }
            public string Perfil { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Senha))
                return BadRequest(new { message = "Email e senha são obrigatórios." });

            var usuario = await _context.Usuarios
                               .Where(u => u.Email.ToLower() == dto.Email.ToLower() && u.Ativo)
                               .FirstOrDefaultAsync();

            if (usuario == null)
                return Unauthorized(new { message = "Credenciais inválidas." });

            // Verifica senha
            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Senha);
            if (resultado == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Credenciais inválidas." });

            // Gera o token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim("perfil", usuario.Perfil ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.TokenLifetimeMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                expiracao = token.ValidTo,
                perfil = usuario.Perfil
        });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
        {
            // 1) Verifica se veio algo nulo/vazio
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Senha))
                return BadRequest(new { message = "Email e senha são obrigatórios." });

            // 2) Checa se já existe um usuário ativo com aquele e-mail
            var emailNorm = dto.Email.Trim().ToLower();
            var usuarioExistente = await _context.Usuarios
                                    .AsNoTracking()
                                    .Where(u => u.Email.ToLower() == emailNorm)
                                    .FirstOrDefaultAsync();

            if (usuarioExistente != null)
                return Conflict(new { message = "Já existe um usuário cadastrado com este e-mail." });

            // 3) Cria a entidade Usuario e aplica o hash da senha
            var novoUsuario = new Usuario
            {
                NomeUsuario = dto.NomeUsuario?.Trim(),
                Email = emailNorm,
                Ativo = true,
                DataCriacao = DateTime.UtcNow,
                Perfil = dto.Perfil
            };

            // Gera o hash da senha
            novoUsuario.PasswordHash = _passwordHasher.HashPassword(novoUsuario, dto.Senha.Trim());

            // 4) Persiste no banco
            _context.Usuarios.Add(novoUsuario);
            await _context.SaveChangesAsync();

            // 5) Retorna Created (201) para indicar que foi criado com sucesso
            //    Como exemplo, retornamos o Id do usuário e o e-mail
            return CreatedAtAction(
                nameof(GetById),
                new { id = novoUsuario.Id },
                new { novoUsuario.Id, novoUsuario.Email, novoUsuario.NomeUsuario, novoUsuario.Perfil }
            );
        }

        // Método auxiliar para que o CreatedAtAction funcione corretamente
        // (opcional — se você não quiser implementar um GET, basta retornar Ok() no Register)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var usuario = await _context.Usuarios
                                 .AsNoTracking()
                                 .Where(u => u.Id == id)
                                 .Select(u => new
                                 {
                                     u.Id,
                                     u.NomeUsuario,
                                     u.Email,
                                     u.DataCriacao,
                                     u.Ativo, 
                                     u.Perfil
                                 })
                                 .FirstOrDefaultAsync();

            if (usuario == null)
                return NotFound();

            return Ok(usuario);
        }
    }
}
