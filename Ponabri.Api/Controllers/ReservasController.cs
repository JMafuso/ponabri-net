using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Data;
using Ponabri.Api.Models;
using Ponabri.Api.Services;
using Swashbuckle.AspNetCore.Annotations; // Para anotações do Swagger
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // GET: api/Reservas/VALIDACAO/{codigoReserva}
        [HttpGet("VALIDACAO/{codigoReserva}")]
        [SwaggerOperation(Summary = "Valida um código de reserva para check-in via IoT")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))] // Use um DTO aqui
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ValidarReservaParaIoT(string codigoReserva)
        {
            var reserva = await _context.Reservas
                                .FirstOrDefaultAsync(r => r.CodigoReserva == codigoReserva && r.Status == ReservaStatus.Ativa);

            if (reserva == null)
            {
                return Ok(new { status = "invalido", mensagem = "Reserva não encontrada ou não está ativa." });
            }

            // Lógica adicional: talvez verificar data, etc.

            return Ok(new { status = "valido", usuarioId = reserva.UsuarioId, abrigoId = reserva.AbrigoId });
        }

        // POST: api/Reservas (Criar uma nova reserva)
        [HttpPost]
        [SwaggerOperation(Summary = "Cria uma nova reserva")]
        public async Task<ActionResult<Reserva>> PostReserva(Reserva reserva) // Idealmente, use um DTO para entrada
        {
            // Validação básica
            var abrigo = await _context.Abrigos.FindAsync(reserva.AbrigoId);
            if (abrigo == null || abrigo.Status != AbrigoStatus.Aberto)
                return BadRequest(new { mensagem = "Abrigo não encontrado ou não está aberto." });

            if (abrigo.VagasPessoasDisponiveis < reserva.QuantidadePessoas || 
                (reserva.UsouVagaCarro && abrigo.VagasCarrosDisponiveis < 1))
            {
                return BadRequest(new { mensagem = "Vagas insuficientes." });
            }

            // Atualiza vagas no abrigo
            abrigo.VagasPessoasDisponiveis -= reserva.QuantidadePessoas;
            if (reserva.UsouVagaCarro)
                abrigo.VagasCarrosDisponiveis -= 1;
            
            if(abrigo.VagasPessoasDisponiveis == 0) abrigo.Status = AbrigoStatus.Lotado;

            // Gera código único para a reserva
            reserva.CodigoReserva = $"PONABRI-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            reserva.DataCriacao = DateTime.UtcNow;
            reserva.Status = ReservaStatus.Ativa;

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            // Publicar mensagem no RabbitMQ
            var eventoReserva = new { ReservaId = reserva.Id, CodigoReserva = reserva.CodigoReserva, Data = DateTime.UtcNow };
            _messageProducer.SendMessage(eventoReserva, "reservas_criadas_queue");

            // Retorna HATEOAS link
            var links = new List<LinkDto>
            {
                new LinkDto(Url.Action(nameof(ValidarReservaParaIoT), "Reservas", new { codigoReserva = reserva.CodigoReserva }, Request.Scheme), "validar_reserva_iot", "GET"),
                new LinkDto(Url.Action(nameof(GetReserva), "Reservas", new { id = reserva.Id }, Request.Scheme), "self", "GET")
            };

            // É bom criar um DTO para o retorno, incluindo os links
            return CreatedAtAction(nameof(GetReserva), new { id = reserva.Id }, new { reserva, links });
        }

        // GET: api/Reservas/{id} - Necessário para o CreatedAtAction
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Obtém uma reserva específica pelo ID")]
        public async Task<ActionResult<Reserva>> GetReserva(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) return NotFound();
            return reserva;
        }
        // Implementar GET (all), PUT, DELETE para completar o CRUD de Reserva
    }

    // DTO para HATEOAS (coloque em uma pasta Dtos)
    public class LinkDto
    {
        public string Href { get; private set; }
        public string Rel { get; private set; }
        public string Method { get; private set; }

        public LinkDto(string href, string rel, string method)
        {
            Href = href;
            Rel = rel;
            Method = method;
        }
    }
} 