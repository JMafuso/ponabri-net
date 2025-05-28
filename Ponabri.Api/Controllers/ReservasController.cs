using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Data;
using Ponabri.Api.Models;
using Ponabri.Api.Services;
using Ponabri.Api.Dtos.ReservaDtos; // Adicionado para DTOs de Reserva
using Ponabri.Api.Dtos.Common; // Adicionado para LinkDto
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // Para [Authorize]
using System.Security.Claims; // Para ClaimTypes
using Microsoft.AspNetCore.Http; // Para StatusCodes

namespace Ponabri.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservasController : ControllerBase
    {
        private readonly PonabriDbContext _context;
        private readonly IMessageProducer _messageProducer;

        public ReservasController(PonabriDbContext context, IMessageProducer messageProducer)
        {
            _context = context;
            _messageProducer = messageProducer;
        }

        // POST: api/Reservas (Criar uma nova reserva)
        [HttpPost]
        [Authorize] // Proteger endpoint
        [SwaggerOperation(Summary = "Cria uma nova reserva para o usuário autenticado")]
        [SwaggerResponse(StatusCodes.Status201Created, "Reserva criada com sucesso", typeof(ReservaResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou vagas insuficientes")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Usuário não autenticado")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Abrigo não encontrado ou não está aberto")]
        public async Task<ActionResult<ReservaResponseDto>> CreateReserva([FromBody] ReservaCreateDto reservaDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Obter ID do usuário autenticado
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Unauthorized("Não foi possível identificar o usuário.");
            }

            var abrigo = await _context.Abrigos.FindAsync(reservaDto.AbrigoId);
            if (abrigo == null || abrigo.Status != AbrigoStatus.Aberto)
            {
                return NotFound(new { mensagem = "Abrigo não encontrado ou não está aberto." });
            }

            if (abrigo.VagasPessoasDisponiveis < reservaDto.QuantidadePessoas)
            {
                return BadRequest(new { mensagem = $"Vagas para pessoas insuficientes. Disponíveis: {abrigo.VagasPessoasDisponiveis}, Solicitadas: {reservaDto.QuantidadePessoas}" });
            }
            if (reservaDto.UsouVagaCarro && abrigo.VagasCarrosDisponiveis < 1)
            {
                return BadRequest(new { mensagem = "Vaga para carro indisponível." });
            }

            // Atualiza vagas no abrigo
            abrigo.VagasPessoasDisponiveis -= reservaDto.QuantidadePessoas;
            if (reservaDto.UsouVagaCarro)
                abrigo.VagasCarrosDisponiveis -= 1;
            
            if(abrigo.VagasPessoasDisponiveis == 0 && abrigo.VagasCarrosDisponiveis == 0) abrigo.Status = AbrigoStatus.Lotado;
            else if (abrigo.VagasPessoasDisponiveis == 0 && !reservaDto.UsouVagaCarro) abrigo.Status = AbrigoStatus.Lotado; // Se lotou pessoas e não usou carro, mas carro pode estar lotado por outros

            var reserva = new Reserva
            {
                UsuarioId = int.Parse(usuarioId),
                AbrigoId = reservaDto.AbrigoId,
                QuantidadePessoas = reservaDto.QuantidadePessoas,
                UsouVagaCarro = reservaDto.UsouVagaCarro,
                CodigoReserva = $"PONABRI-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                DataCriacao = DateTime.UtcNow,
                Status = ReservaStatus.Ativa
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            // Publicar mensagem no RabbitMQ
            var eventoReserva = new { ReservaId = reserva.Id, CodigoReserva = reserva.CodigoReserva, UsuarioId = usuarioId, Data = DateTime.UtcNow };
            _messageProducer.SendMessage(eventoReserva, "reservas_criadas_queue");

            var responseDto = await MapToReservaResponseDto(reserva);
            responseDto.Links.Add(new LinkDto(Url.Action(nameof(GetReservaById), "Reservas", new { id = reserva.Id }, Request.Scheme), "self", "GET"));
            responseDto.Links.Add(new LinkDto(Url.Action(nameof(ValidarReservaParaIoT), "Reservas", new { codigoReserva = reserva.CodigoReserva }, Request.Scheme), "validar_reserva_iot", "GET"));
            responseDto.Links.Add(new LinkDto(Url.Action(nameof(CancelarReserva), "Reservas", new { id = reserva.Id }, Request.Scheme), "cancelar_reserva", "PUT"));

            return CreatedAtAction(nameof(GetReservaById), new { id = reserva.Id }, responseDto);
        }

        // GET: api/Reservas/{id}
        [HttpGet("{id}", Name = "GetReservaById")]
        [Authorize] // Requer autenticação
        [SwaggerOperation(Summary = "Obtém uma reserva específica pelo ID.", Description = "Usuários obtêm suas próprias reservas. Administradores obtêm qualquer reserva.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Reserva encontrada", typeof(ReservaResponseDto))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado à reserva de outro usuário")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Reserva não encontrada")]
        public async Task<ActionResult<ReservaResponseDto>> GetReservaById(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(UserRoles.Admin);

            var reserva = await _context.Reservas
                .Include(r => r.Usuario) 
                .Include(r => r.Abrigo)  
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null) return NotFound();

            if (!isAdmin && reserva.UsuarioId.ToString() != usuarioId)
            {
                return Forbid("Você não tem permissão para acessar esta reserva.");
            }
            
            return Ok(await MapToReservaResponseDto(reserva));
        }

        // GET: api/Reservas
        [HttpGet]
        [Authorize] // Requer autenticação
        [SwaggerOperation(Summary = "Lista reservas.", Description = "Usuários listam suas próprias reservas. Administradores listam todas as reservas.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Lista de reservas retornada", typeof(List<ReservaResponseDto>))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado")]
        public async Task<ActionResult<IEnumerable<ReservaResponseDto>>> GetReservas([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(UserRoles.Admin);
            
            var query = _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Abrigo)
                .OrderByDescending(r => r.DataCriacao)
                .AsQueryable(); // Inicia a query
            
            if (!isAdmin) // Se não for admin, filtra pelo usuário autenticado
            {
                query = query.Where(r => r.UsuarioId.ToString() == usuarioId);
            }

            var totalItems = await query.CountAsync();
            var reservas = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Response.Headers.Append("X-Total-Count", totalItems.ToString());
            Response.Headers.Append("X-Page-Number", pageNumber.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());

            var responseDtos = new List<ReservaResponseDto>();
            foreach(var reserva in reservas)
            {
                responseDtos.Add(await MapToReservaResponseDto(reserva));
            }
            return Ok(responseDtos);
        }

        // PUT: api/Reservas/{id}/cancelar
        [HttpPut("{id}/cancelar")]
        [Authorize] // Requer autenticação
        [SwaggerOperation(Summary = "Cancela uma reserva ativa.", Description = "Usuários cancelam suas próprias reservas. Administradores cancelam qualquer reserva ativa.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Reserva cancelada com sucesso", typeof(ReservaResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Reserva não pode ser cancelada (ex: já cancelada, concluída)")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado para cancelar reserva de outro usuário")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Reserva ou abrigo associado não encontrado")]
        public async Task<ActionResult<ReservaResponseDto>> CancelarReserva(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(UserRoles.Admin);
            var reserva = await _context.Reservas.Include(r => r.Abrigo).FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null) return NotFound("Reserva não encontrada.");

            if (!isAdmin && reserva.UsuarioId.ToString() != usuarioId)
            {
                return Forbid("Você não tem permissão para cancelar esta reserva.");
            }

            if (reserva.Status != ReservaStatus.Ativa)
            {
                return BadRequest(new { mensagem = $"Apenas reservas ativas podem ser canceladas. Status atual: {reserva.Status}" });
            }

            var abrigo = reserva.Abrigo; 
            if (abrigo == null) 
            {
                return NotFound(new { mensagem = "Abrigo associado à reserva não encontrado. Não é possível ajustar as vagas." });
            }

            reserva.Status = ReservaStatus.Cancelada;
            
            abrigo.VagasPessoasDisponiveis += reserva.QuantidadePessoas;
            if (reserva.UsouVagaCarro)
                abrigo.VagasCarrosDisponiveis += 1;
            
            if (abrigo.Status == AbrigoStatus.Lotado)
            {
                abrigo.Status = AbrigoStatus.Aberto;
            }

            await _context.SaveChangesAsync();
            return Ok(await MapToReservaResponseDto(reserva));
        }

        // GET: api/Reservas/VALIDACAO/{codigoReserva}
        [HttpGet("VALIDACAO/{codigoReserva}")]
        [SwaggerOperation(Summary = "Valida um código de reserva para check-in via IoT (Não requer autenticação de usuário final)")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))] 
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ValidarReservaParaIoT(string codigoReserva)
        {
            var reserva = await _context.Reservas
                                .Include(r => r.Usuario)
                                .Include(r => r.Abrigo)
                                .FirstOrDefaultAsync(r => r.CodigoReserva == codigoReserva && r.Status == ReservaStatus.Ativa);

            if (reserva == null)
            {
                return Ok(new { status = "invalido", mensagem = "Reserva não encontrada ou não está ativa." });
            }
            // Aqui você pode querer mudar o status da reserva para Concluida se a validação for um check-in bem sucedido.
            // Ex: reserva.Status = ReservaStatus.Concluida; await _context.SaveChangesAsync();
            // Ou pode ser apenas uma validação sem alteração de estado, dependendo do fluxo do IoT.

            return Ok(new 
            {
                status = "valido", 
                reservaId = reserva.Id,
                codigoReserva = reserva.CodigoReserva,
                usuarioId = reserva.UsuarioId,
                usuarioNome = reserva.Usuario?.Nome, // Null check
                abrigoId = reserva.AbrigoId,
                abrigoNome = reserva.Abrigo?.NomeLocal, // Null check
                quantidadePessoas = reserva.QuantidadePessoas,
                usouVagaCarro = reserva.UsouVagaCarro
            });
        }

        // Método auxiliar para mapear Reserva para ReservaResponseDto
        private async Task<ReservaResponseDto> MapToReservaResponseDto(Reserva reserva)
        {
            // Carregar explicitamente o usuário e abrigo se não foram incluídos na consulta original
            // No entanto, é melhor usar .Include() nas consultas Linq como feito acima.
            var usuario = reserva.Usuario ?? await _context.Usuarios.FindAsync(reserva.UsuarioId);
            var abrigo = reserva.Abrigo ?? await _context.Abrigos.FindAsync(reserva.AbrigoId);

            return new ReservaResponseDto
            {
                Id = reserva.Id,
                CodigoReserva = reserva.CodigoReserva,
                UsuarioId = reserva.UsuarioId,
                Usuario = usuario != null ? new UsuarioInfoForReservaDto { Id = usuario.Id, Nome = usuario.Nome, Email = usuario.Email } : null,
                AbrigoId = reserva.AbrigoId,
                Abrigo = abrigo != null ? new AbrigoInfoForReservaDto { Id = abrigo.Id, NomeLocal = abrigo.NomeLocal, Endereco = abrigo.Endereco } : null,
                QuantidadePessoas = reserva.QuantidadePessoas,
                UsouVagaCarro = reserva.UsouVagaCarro,
                DataCriacao = reserva.DataCriacao,
                Status = reserva.Status,
                Links = new List<LinkDto>() // Inicializa a lista, os links são adicionados pelo chamador
            };
        }
    }
} 