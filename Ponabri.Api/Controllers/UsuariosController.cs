using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Data;
using Ponabri.Api.Models;
using Ponabri.Api.Dtos.UsuarioDtos;
using System.Threading.Tasks;
using BCrypt.Net; // Para hashing de senha
using Microsoft.Extensions.Configuration; // Para ler appsettings.json para JWT
using Microsoft.IdentityModel.Tokens; // Para JWT
using System.IdentityModel.Tokens.Jwt; // Para JWT
using System.Security.Claims; // Para JWT
using System.Text; // Para JWT
using System;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic; // Para List em GET all
using Microsoft.AspNetCore.Authorization; // Para proteger endpoints

namespace Ponabri.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly PonabriDbContext _context;
        private readonly IConfiguration _configuration;

        public UsuariosController(PonabriDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration; // Injetar IConfiguration
        }

        // POST: api/Usuarios/registrar
        [HttpPost("registrar")]
        [SwaggerOperation(Summary = "Registra um novo usuário com o papel padrão 'User'")]
        [SwaggerResponse(StatusCodes.Status201Created, "Usuário registrado com sucesso", typeof(UsuarioResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou email já cadastrado")]
        public async Task<ActionResult<UsuarioResponseDto>> Registrar([FromBody] UsuarioRegisterDto usuarioRegisterDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Usuarios.AnyAsync(u => u.Email == usuarioRegisterDto.Email))
            {
                return BadRequest("Email já cadastrado.");
            }

            string senhaHash = BCrypt.Net.BCrypt.HashPassword(usuarioRegisterDto.Senha);

            var usuario = new Usuario
            {
                Nome = usuarioRegisterDto.Nome,
                Email = usuarioRegisterDto.Email,
                Senha = senhaHash,
                Role = UserRoles.User // Atribuir papel padrão no registro
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var usuarioResponseDto = new UsuarioResponseDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email
                // Não retornamos a Role aqui, mas o token de login a conterá.
            };

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuarioResponseDto);
        }

        // POST: api/Usuarios/login
        [HttpPost("login")]
        [SwaggerOperation(Summary = "Realiza o login do usuário e retorna um token JWT contendo a Role")]
        [SwaggerResponse(StatusCodes.Status200OK, "Login bem-sucedido", typeof(TokenResponseDto))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Email ou senha inválidos")]
        public async Task<ActionResult<TokenResponseDto>> Login([FromBody] UsuarioLoginDto usuarioLoginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == usuarioLoginDto.Email);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(usuarioLoginDto.Senha, usuario.Senha))
            {
                return Unauthorized("Email ou senha inválidos.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["JwtSettings:Key"];
            if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32) 
            {
                Console.Error.WriteLine("Chave JWT não configurada corretamente ou é muito curta.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro na configuração de autenticação.");
            }
            var key = Encoding.ASCII.GetBytes(jwtKey);
            
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())             
            };

            if (!string.IsNullOrEmpty(usuario.Role)) // Adicionar claim de Role se existir
            {
                claims.Add(new Claim(ClaimTypes.Role, usuario.Role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new TokenResponseDto { Token = tokenString });
        }

        // GET: api/Usuarios/{id}
        [HttpGet("{id}", Name = "GetUsuario")] 
        [Authorize] 
        [SwaggerOperation(Summary = "Obtém um usuário específico pelo ID (protegido)", Description = "Requer autenticação. Um usuário pode obter seus próprios dados. Administradores podem obter dados de qualquer usuário.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Usuário encontrado", typeof(UsuarioResponseDto))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado")]
        public async Task<ActionResult<UsuarioResponseDto>> GetUsuario(int id)
        {
            var solicitanteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(UserRoles.Admin);

            if (!isAdmin && (solicitanteId == null || id.ToString() != solicitanteId))
            {
                return Forbid("Acesso negado. Você só pode visualizar seus próprios dados."); 
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            
            return Ok(new UsuarioResponseDto { Id = usuario.Id, Nome = usuario.Nome, Email = usuario.Email });
        }

        // GET: api/Usuarios
        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)] // Somente Admin pode listar todos os usuários
        [SwaggerOperation(Summary = "Obtém uma lista paginada de todos os usuários (protegido - Admin)", Description = "Requer autenticação e papel de Administrador.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Lista de usuários retornada", typeof(List<UsuarioResponseDto>))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado. Requer papel de Administrador.")]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100; 

            var query = _context.Usuarios.AsQueryable();
            
            var totalItems = await query.CountAsync();
            var usuarios = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UsuarioResponseDto { Id = u.Id, Nome = u.Nome, Email = u.Email })
                .ToListAsync();
            
            Response.Headers.Append("X-Total-Count", totalItems.ToString());
            Response.Headers.Append("X-Page-Number", pageNumber.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());

            return Ok(usuarios);
        }

        // PUT: api/Usuarios/{id}
        [HttpPut("{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Atualiza os dados de um usuário (protegido)", Description = "Requer autenticação. Um usuário pode atualizar seus próprios dados. Administradores podem atualizar dados de qualquer usuário.")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Usuário atualizado com sucesso")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou email já em uso")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
        public async Task<IActionResult> PutUsuario(int id, [FromBody] UsuarioUpdateDto usuarioUpdateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var solicitanteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(UserRoles.Admin);

            if (!isAdmin && (solicitanteId == null || id.ToString() != solicitanteId))
            {
                return Forbid("Acesso negado. Você só pode atualizar seus próprios dados."); 
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(usuarioUpdateDto.Nome))
            {
                usuario.Nome = usuarioUpdateDto.Nome;
            }
            if (!string.IsNullOrWhiteSpace(usuarioUpdateDto.Email) && usuario.Email != usuarioUpdateDto.Email)
            {
                if (await _context.Usuarios.AnyAsync(u => u.Email == usuarioUpdateDto.Email && u.Id != id))
                {
                    return BadRequest("Este email já está em uso por outra conta.");
                }
                usuario.Email = usuarioUpdateDto.Email;
            }
            // Admins não devem mudar a role por este endpoint para evitar auto-promoção acidental sem um fluxo dedicado.
            // A alteração de Role deve ser uma ação administrativa separada e mais controlada.

            _context.Entry(usuario).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Usuarios.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        // DELETE: api/Usuarios/{id}
        [HttpDelete("{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Exclui um usuário (protegido)", Description = "Requer autenticação. Um usuário pode excluir sua própria conta. Administradores podem excluir qualquer usuário.")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Usuário excluído com sucesso")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var solicitanteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(UserRoles.Admin);
            var usuarioSendoExcluido = await _context.Usuarios.FindAsync(id);

            if (usuarioSendoExcluido == null) return NotFound();

            if (!isAdmin && (solicitanteId == null || id.ToString() != solicitanteId))
            {
                return Forbid("Acesso negado. Você só pode excluir sua própria conta.");
            }

            // Evitar que um admin se auto-exclua se for o único admin (precisaria de lógica mais complexa)
            // if (isAdmin && id.ToString() == solicitanteId && (await _context.Usuarios.CountAsync(u => u.Role == UserRoles.Admin)) <= 1)
            // {
            //     return BadRequest("Não é possível excluir o único administrador.");
            // }

            _context.Usuarios.Remove(usuarioSendoExcluido);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/usuarios/{id}/promoteToAdmin
        [HttpPost("{id}/promoteToAdmin")]
        [Authorize(Roles = UserRoles.Admin)] // Apenas Admins podem promover outros usuários
        [SwaggerOperation(Summary = "Promove um usuário para o papel de Administrador (protegido - Admin)")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Usuário promovido a Administrador com sucesso")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Usuário já é Administrador")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado. Requer papel de Administrador.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
        public async Task<IActionResult> PromoteToAdmin(int id)
        {
            var usuarioParaPromover = await _context.Usuarios.FindAsync(id);

            if (usuarioParaPromover == null)
            {
                return NotFound(new { message = "Usuário a ser promovido não encontrado." });
            }

            if (usuarioParaPromover.Role == UserRoles.Admin)
            {
                return BadRequest(new { message = "Este usuário já é um Administrador." });
            }

            usuarioParaPromover.Role = UserRoles.Admin;
            _context.Entry(usuarioParaPromover).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
} 