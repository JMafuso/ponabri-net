using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Data;
using Ponabri.Api.Models;
using Ponabri.Api.Dtos;
using Ponabri.Api.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
        public async Task<ActionResult<IEnumerable<Abrigo>>> GetAbrigos()
        {
            return await _context.Abrigos.ToListAsync();
        }

        // GET: api/Abrigos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Abrigo>> GetAbrigo(int id)
        {
            var abrigo = await _context.Abrigos.FindAsync(id);

            if (abrigo == null)
            {
                return NotFound();
            }

            return abrigo;
        }

        // POST: api/Abrigos
        [HttpPost]
        [SwaggerOperation(Summary = "Cria um novo abrigo", Description = "Recebe os dados de um abrigo utilizando um DTO, sugere uma categoria via ML.NET e o armazena no banco de dados.")]
        [SwaggerResponse(StatusCodes.Status201Created, "Retorna o abrigo recém-criado.", typeof(Abrigo))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Se os dados do abrigo forem inválidos ou a requisição estiver malformada.")]
        public async Task<ActionResult<Abrigo>> PostAbrigo([FromBody] AbrigoCreateDto abrigoDto)
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
            else
            {
                abrigo.CategoriaSugeridaML = null;
            }
            
            _context.Abrigos.Add(abrigo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAbrigo), new { id = abrigo.Id }, abrigo);
        }

        // PUT: api/Abrigos/5
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Atualiza um abrigo existente", Description = "Permite a atualização parcial de um abrigo. Apenas os campos fornecidos no corpo da requisição serão atualizados.")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Abrigo atualizado com sucesso.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Se os dados forem inválidos, ou a nova capacidade for menor que as vagas ocupadas.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Se o abrigo com o ID especificado não for encontrado.")]
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

            if (abrigoDto.NomeLocal != null)
            {
                abrigoParaAtualizar.NomeLocal = abrigoDto.NomeLocal;
            }
            if (abrigoDto.Endereco != null)
            {
                abrigoParaAtualizar.Endereco = abrigoDto.Endereco;
            }
            if (abrigoDto.Regiao != null)
            {
                abrigoParaAtualizar.Regiao = abrigoDto.Regiao;
            }

            if (abrigoDto.CapacidadePessoas.HasValue)
            {
                int vagasOcupadasPessoas = abrigoOriginal.CapacidadePessoas - abrigoOriginal.VagasPessoasDisponiveis;
                if (abrigoDto.CapacidadePessoas.Value < vagasOcupadasPessoas)
                {
                    return BadRequest($"Nova capacidade de pessoas ({abrigoDto.CapacidadePessoas.Value}) não pode ser menor que o número de vagas já ocupadas ({vagasOcupadasPessoas}).");
                }
                abrigoParaAtualizar.CapacidadePessoas = abrigoDto.CapacidadePessoas.Value;
                abrigoParaAtualizar.VagasPessoasDisponiveis = abrigoParaAtualizar.CapacidadePessoas - vagasOcupadasPessoas;
            }

            if (abrigoDto.CapacidadeCarros.HasValue)
            {
                int vagasOcupadasCarros = abrigoOriginal.CapacidadeCarros - abrigoOriginal.VagasCarrosDisponiveis;
                if (abrigoDto.CapacidadeCarros.Value < vagasOcupadasCarros)
                {
                    return BadRequest($"Nova capacidade de carros ({abrigoDto.CapacidadeCarros.Value}) não pode ser menor que o número de vagas já ocupadas ({vagasOcupadasCarros}).");
                }
                abrigoParaAtualizar.CapacidadeCarros = abrigoDto.CapacidadeCarros.Value;
                abrigoParaAtualizar.VagasCarrosDisponiveis = abrigoParaAtualizar.CapacidadeCarros - vagasOcupadasCarros;
            }

            if (abrigoDto.ContatoResponsavel != null)
            {
                abrigoParaAtualizar.ContatoResponsavel = abrigoDto.ContatoResponsavel;
            }
            if (abrigoDto.Descricao != null)
            {
                abrigoParaAtualizar.Descricao = abrigoDto.Descricao;
                if (!string.IsNullOrEmpty(abrigoParaAtualizar.Descricao))
                {
                    abrigoParaAtualizar.CategoriaSugeridaML = _shelterCategoryService.PredictCategory(abrigoParaAtualizar.Descricao);
                }
                else
                {
                    abrigoParaAtualizar.CategoriaSugeridaML = null;
                }
            }
            if (abrigoDto.Status.HasValue)
            {
                abrigoParaAtualizar.Status = abrigoDto.Status.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AbrigoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Abrigos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAbrigo(int id)
        {
            var abrigo = await _context.Abrigos.FindAsync(id);
            if (abrigo == null)
            {
                return NotFound();
            }

            _context.Abrigos.Remove(abrigo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AbrigoExists(int id)
        {
            return _context.Abrigos.Any(e => e.Id == id);
        }
    }
} 