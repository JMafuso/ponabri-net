using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Data;
using Ponabri.Api.Models;
using Ponabri.Api.Dtos.AbrigoDtos;
using Ponabri.Api.Dtos.Common;
using Ponabri.Api.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Ponabri.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AbrigosController : ControllerBase
    {
        private readonly PonabriDbContext _context;
        private readonly ShelterCategoryService _shelterCategoryService;

        public AbrigosController(PonabriDbContext context, ShelterCategoryService shelterCategoryService)
        {
            _context = context;
            _shelterCategoryService = shelterCategoryService;
        }

        // GET: api/Abrigos
        [HttpGet]
        [SwaggerOperation(Summary = "Lista todos os abrigos disponíveis com informações resumidas")]
        [SwaggerResponse(StatusCodes.Status200OK, "Lista de abrigos retornada", typeof(IEnumerable<AbrigoSummaryDto>))]
        public async Task<ActionResult<IEnumerable<AbrigoSummaryDto>>> GetAbrigos()
        {
            var abrigos = await _context.Abrigos.ToListAsync();
            var dtos = abrigos.Select(abrigo => MapToAbrigoSummaryDto(abrigo)).ToList();
            return Ok(dtos);
        }

        // GET: api/Abrigos/5
        [HttpGet("{id}", Name = "GetAbrigoById")]
        [SwaggerOperation(Summary = "Obtém detalhes de um abrigo específico pelo ID")]
        [SwaggerResponse(StatusCodes.Status200OK, "Detalhes do abrigo retornados", typeof(AbrigoDetailsDto))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Abrigo não encontrado")]
        public async Task<ActionResult<AbrigoDetailsDto>> GetAbrigo(int id)
        {
            var abrigo = await _context.Abrigos.FindAsync(id);

            if (abrigo == null)
            {
                return NotFound();
            }
            var solicitanteRole = (HttpContext?.User?.Identity?.IsAuthenticated ?? false) 
                                  ? User.FindFirstValue(ClaimTypes.Role) 
                                  : null;
            return Ok(MapToAbrigoDetailsDto(abrigo, solicitanteRole));
        }

        // POST: api/Abrigos
        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        [SwaggerOperation(Summary = "Cria um novo abrigo (Admin)", Description = "Recebe os dados de um abrigo, sugere uma categoria via ML.NET e o armazena.")]
        [SwaggerResponse(StatusCodes.Status201Created, "Retorna o abrigo recém-criado com detalhes", typeof(AbrigoDetailsDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado.")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado. Requer papel de Administrador.")]
        public async Task<ActionResult<AbrigoDetailsDto>> PostAbrigo([FromBody] AbrigoCreateDto abrigoDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var abrigo = new Abrigo
            {
                NomeLocal = abrigoDto.NomeLocal,
                Endereco = abrigoDto.Endereco,
                Regiao = abrigoDto.Regiao,
                CapacidadePessoas = abrigoDto.CapacidadePessoas,
                VagasPessoasDisponiveis = abrigoDto.CapacidadePessoas,
                CapacidadeCarros = abrigoDto.CapacidadeCarros,
                VagasCarrosDisponiveis = abrigoDto.CapacidadeCarros,
                ContatoResponsavel = abrigoDto.ContatoResponsavel,
                Descricao = abrigoDto.Descricao,
                Status = AbrigoStatus.Aberto
            };

            if (!string.IsNullOrEmpty(abrigo.Descricao))
            {
                abrigo.CategoriaSugeridaML = _shelterCategoryService.PredictCategory(abrigo.Descricao);
            }
            
            _context.Abrigos.Add(abrigo);
            await _context.SaveChangesAsync();

            var responseDto = MapToAbrigoDetailsDto(abrigo, UserRoles.Admin);
            return CreatedAtAction(nameof(GetAbrigo), new { id = abrigo.Id }, responseDto);
        }

        // PUT: api/Abrigos/5
        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.Admin)]
        [SwaggerOperation(Summary = "Atualiza um abrigo existente (Admin)", Description = "Permite a atualização parcial de um abrigo.")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Abrigo atualizado com sucesso.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou capacidade menor que ocupação.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado.")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado. Requer papel de Administrador.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Abrigo não encontrado.")]
        public async Task<IActionResult> PutAbrigo(int id, [FromBody] AbrigoUpdateDto abrigoDto)
        {
            if (!ModelState.IsValid) 
            {
                return BadRequest(ModelState);
            }

            var abrigoOriginal = await _context.Abrigos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (abrigoOriginal == null)
            {
                return NotFound("Abrigo original não encontrado para atualização.");
            }

            var abrigoParaAtualizar = await _context.Abrigos.FindAsync(id);
            if (abrigoParaAtualizar == null)
            {
                return NotFound("Abrigo para atualizar não encontrado.");
            }

            if (abrigoDto.NomeLocal != null) abrigoParaAtualizar.NomeLocal = abrigoDto.NomeLocal;
            if (abrigoDto.Endereco != null) abrigoParaAtualizar.Endereco = abrigoDto.Endereco;
            if (abrigoDto.Regiao != null) abrigoParaAtualizar.Regiao = abrigoDto.Regiao;

            if (abrigoDto.CapacidadePessoas.HasValue)
            {
                int vagasOcupadasPessoas = abrigoOriginal.CapacidadePessoas - abrigoOriginal.VagasPessoasDisponiveis;
                if (abrigoDto.CapacidadePessoas.Value < vagasOcupadasPessoas)
                    return BadRequest($"Nova capacidade de pessoas ({abrigoDto.CapacidadePessoas.Value}) não pode ser menor que as vagas já ocupadas ({vagasOcupadasPessoas}).");
                abrigoParaAtualizar.CapacidadePessoas = abrigoDto.CapacidadePessoas.Value;
                abrigoParaAtualizar.VagasPessoasDisponiveis = abrigoParaAtualizar.CapacidadePessoas - vagasOcupadasPessoas;
            }

            if (abrigoDto.CapacidadeCarros.HasValue)
            {
                int vagasOcupadasCarros = abrigoOriginal.CapacidadeCarros - abrigoOriginal.VagasCarrosDisponiveis;
                if (abrigoDto.CapacidadeCarros.Value < vagasOcupadasCarros)
                    return BadRequest($"Nova capacidade de carros ({abrigoDto.CapacidadeCarros.Value}) não pode ser menor que as vagas já ocupadas ({vagasOcupadasCarros}).");
                abrigoParaAtualizar.CapacidadeCarros = abrigoDto.CapacidadeCarros.Value;
                abrigoParaAtualizar.VagasCarrosDisponiveis = abrigoParaAtualizar.CapacidadeCarros - vagasOcupadasCarros;
            }

            if (abrigoDto.ContatoResponsavel != null) abrigoParaAtualizar.ContatoResponsavel = abrigoDto.ContatoResponsavel;
            if (abrigoDto.Descricao != null)
            {
                abrigoParaAtualizar.Descricao = abrigoDto.Descricao;
                abrigoParaAtualizar.CategoriaSugeridaML = string.IsNullOrEmpty(abrigoParaAtualizar.Descricao) ? null : _shelterCategoryService.PredictCategory(abrigoParaAtualizar.Descricao);
            }
            if (abrigoDto.Status.HasValue) abrigoParaAtualizar.Status = abrigoDto.Status.Value;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Abrigos/5
        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.Admin)]
        [SwaggerOperation(Summary = "Exclui um abrigo (Admin)")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Abrigo excluído com sucesso.")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autenticado.")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado. Requer papel de Administrador.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Abrigo não encontrado.")]
        public async Task<IActionResult> DeleteAbrigo(int id)
        {
            var abrigo = await _context.Abrigos.FindAsync(id);
            if (abrigo == null) return NotFound();
            _context.Abrigos.Remove(abrigo);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool AbrigoExists(int id) => _context.Abrigos.Any(e => e.Id == id);

        private AbrigoSummaryDto MapToAbrigoSummaryDto(Abrigo abrigo)
        {
            var dto = new AbrigoSummaryDto
            {
                Id = abrigo.Id,
                NomeLocal = abrigo.NomeLocal,
                Endereco = abrigo.Endereco,
                Status = abrigo.Status,
                VagasPessoasDisponiveis = abrigo.VagasPessoasDisponiveis,
                VagasCarrosDisponiveis = abrigo.VagasCarrosDisponiveis
            };

            var selfUrl = Url.Action(nameof(GetAbrigo), "Abrigos", new { id = abrigo.Id }, Request.Scheme);
            if (selfUrl != null) dto.Links.Add(new LinkDto(selfUrl, "self", "GET"));
            
            return dto;
        }

        private AbrigoDetailsDto MapToAbrigoDetailsDto(Abrigo abrigo, string? solicitanteRole)
        {
            var dto = new AbrigoDetailsDto
            {
                Id = abrigo.Id,
                NomeLocal = abrigo.NomeLocal,
                Endereco = abrigo.Endereco,
                Regiao = abrigo.Regiao,
                CapacidadePessoas = abrigo.CapacidadePessoas,
                VagasPessoasDisponiveis = abrigo.VagasPessoasDisponiveis,
                CapacidadeCarros = abrigo.CapacidadeCarros,
                VagasCarrosDisponiveis = abrigo.VagasCarrosDisponiveis,
                ContatoResponsavel = abrigo.ContatoResponsavel,
                Descricao = abrigo.Descricao,
                CategoriaSugeridaML = abrigo.CategoriaSugeridaML,
                Status = abrigo.Status
            };

            var selfUrl = Url.Action(nameof(GetAbrigo), "Abrigos", new { id = abrigo.Id }, Request.Scheme);
            if (selfUrl != null) dto.Links.Add(new LinkDto(selfUrl, "self", "GET"));

            if (solicitanteRole == UserRoles.Admin)
            {
                var updateUrl = Url.Action(nameof(PutAbrigo), "Abrigos", new { id = abrigo.Id }, Request.Scheme);
                if (updateUrl != null) dto.Links.Add(new LinkDto(updateUrl, "update_abrigo", "PUT"));

                var deleteUrl = Url.Action(nameof(DeleteAbrigo), "Abrigos", new { id = abrigo.Id }, Request.Scheme);
                if (deleteUrl != null) dto.Links.Add(new LinkDto(deleteUrl, "delete_abrigo", "DELETE"));
            }

            if (abrigo.Status == AbrigoStatus.Aberto)
            {
                var criarReservaUrl = Url.Action(nameof(ReservasController.CreateReserva), "Reservas");
                if (criarReservaUrl != null) dto.Links.Add(new LinkDto(criarReservaUrl, "criar_reserva_abrigo", "POST"));
            }
            return dto;
        }
    }
} 