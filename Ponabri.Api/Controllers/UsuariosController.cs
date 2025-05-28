using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Data;
using Ponabri.Api.Models;
using Ponabri.Api.Dtos.UsuarioDtos;
using Ponabri.Api.Dtos.Common; // Adicionado para LinkDto
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
            _configuration = configuration; 
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
                Nome = usuarioRegisterDto.NomeCompleto,
                Email = usuarioRegisterDto.Email,
                Senha = senhaHash,
                Role = UserRoles.User 
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            
            var dto = MapToUsuarioResponseDto(usuario, null, null, true);
            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, dto);
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
                // Logar o erro e/ou retornar um erro interno do servidor
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
                Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["JwtSettings:ExpirationHours"] ?? "2")), // Tempo de expiração do token
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
            // Obtem a role do solicitante para passar ao método de mapeamento
            var solicitanteRole = User.FindFirstValue(ClaimTypes.Role); 

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            // Verifica permissão: ou é admin, ou está acessando os próprios dados
            if (!isAdmin && (solicitanteId == null || usuario.Id.ToString() != solicitanteId))
            {
                return Forbid("Acesso negado. Você só pode visualizar seus próprios dados."); 
            }
            
            var dto = MapToUsuarioResponseDto(usuario, solicitanteId, solicitanteRole);
            return Ok(dto);
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

            // O solicitante é garantidamente Admin devido ao [Authorize(Roles = UserRoles.Admin)]
            var solicitanteId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var solicitanteRole = UserRoles.Admin; 

            var query = _context.Usuarios.AsQueryable();
            
            var totalItems = await query.CountAsync();
            var usuarios = await query
                .OrderBy(u => u.Id) // Adiciona uma ordenação padrão
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            Response.Headers.Append("X-Total-Count", totalItems.ToString());
            Response.Headers.Append("X-Page-Number", pageNumber.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());

            var dtos = new List<UsuarioResponseDto>();
            foreach (var usuario in usuarios)
            {
                // Ao listar todos, o "solicitante" é o admin, então passamos esses dados.
                dtos.Add(MapToUsuarioResponseDto(usuario, solicitanteId, solicitanteRole));
            }
            return Ok(dtos);
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

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            // Verifica permissão
            if (!isAdmin && (solicitanteId == null || usuario.Id.ToString() != solicitanteId))
            {
                return Forbid("Acesso negado. Você só pode atualizar seus próprios dados."); 
            }

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

            // Verifica permissão
            if (!isAdmin && (solicitanteId == null || usuarioSendoExcluido.Id.ToString() != solicitanteId))
            {
                return Forbid("Acesso negado. Você só pode excluir sua própria conta.");
            }

            // Lógica para evitar que o único admin se auto-exclua (opcional)
            // if (isAdmin && usuarioSendoExcluido.Id.ToString() == solicitanteId && 
            //     (await _context.Usuarios.CountAsync(u => u.Role == UserRoles.Admin)) <= 1)
            // {
            //     return BadRequest("Não é possível excluir o único administrador do sistema.");
            // }

            _context.Usuarios.Remove(usuarioSendoExcluido);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/usuarios/{id}/promoteToAdmin
        [HttpPost("{id}/promoteToAdmin")]
        [Authorize(Roles = UserRoles.Admin)] 
        [SwaggerOperation(Summary = "Promove um usuário para o papel de Administrador (protegido - Admin)")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Usuário promovido a Administrador com sucesso")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Usuário já é Administrador ou o usuário especificado é o próprio solicitante")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado. Requer papel de Administrador.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
        public async Task<IActionResult> PromoteToAdmin(int id)
        {
            var solicitanteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id.ToString() == solicitanteId)
            {
                 return BadRequest(new { message = "Um administrador não pode se auto-promover (ou re-promover)." });
            }

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

        private UsuarioResponseDto MapToUsuarioResponseDto(Usuario usuario, string? solicitanteId = null, string? solicitanteRole = null, bool isNewUserRegistration = false)
        {
            if (!isNewUserRegistration && (HttpContext?.User?.Identity?.IsAuthenticated ?? false))
            {
                solicitanteId ??= User.FindFirstValue(ClaimTypes.NameIdentifier);
                solicitanteRole ??= User.FindFirstValue(ClaimTypes.Role);
            }
            
            var dto = new UsuarioResponseDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Role = usuario.Role 
            };

            var selfUrl = Url.Action(nameof(GetUsuario), "Usuarios", new { id = usuario.Id }, Request.Scheme);
            if (selfUrl != null) dto.Links.Add(new LinkDto(selfUrl, "self", "GET"));
            
            bool podeGerenciar = (solicitanteRole == UserRoles.Admin) || (!string.IsNullOrEmpty(solicitanteId) && usuario.Id.ToString() == solicitanteId);

            if (podeGerenciar)
            {
                var updateUrl = Url.Action(nameof(PutUsuario), "Usuarios", new { id = usuario.Id }, Request.Scheme);
                if (updateUrl != null) dto.Links.Add(new LinkDto(updateUrl, "update_usuario", "PUT"));

                var deleteUrl = Url.Action(nameof(DeleteUsuario), "Usuarios", new { id = usuario.Id }, Request.Scheme);
                if (deleteUrl != null) dto.Links.Add(new LinkDto(deleteUrl, "delete_usuario", "DELETE"));
            }

            if (solicitanteRole == UserRoles.Admin && usuario.Role != UserRoles.Admin && usuario.Id.ToString() != solicitanteId)
            {
                var promoteUrl = Url.Action(nameof(PromoteToAdmin), "Usuarios", new { id = usuario.Id }, Request.Scheme);
                if (promoteUrl != null) dto.Links.Add(new LinkDto(promoteUrl, "promote_to_admin", "POST"));
            }
            
            if (podeGerenciar) 
            {
                var routeValues = (solicitanteRole == UserRoles.Admin && solicitanteId != usuario.Id.ToString()) 
                                    ? new { usuarioId = usuario.Id } 
                                    : null; 
                var listReservasUrl = Url.Action(nameof(ReservasController.GetReservas), "Reservas", routeValues, Request.Scheme);
                if (listReservasUrl != null) dto.Links.Add(new LinkDto(listReservasUrl, "listar_reservas_usuario", "GET"));
            }

            return dto;
        }
    }
} 