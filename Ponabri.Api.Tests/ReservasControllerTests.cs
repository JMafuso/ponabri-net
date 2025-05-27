using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Controllers;
using Ponabri.Api.Data;
using Ponabri.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ponabri.Api.Tests
{
    public class ReservasControllerTests
    {
        private PonabriDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<PonabriDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name for each test run
                .Options;
            var dbContext = new PonabriDbContext(options);
            // Seed data if necessary
            dbContext.Abrigos.Add(new Abrigo { Id = 1, NomeLocal = "Abrigo Teste", VagasPessoasDisponiveis = 10, VagasCarrosDisponiveis = 2, Status = AbrigoStatus.Aberto, Regiao = "Teste" });
            dbContext.Usuarios.Add(new Usuario { Id = 1, Nome = "Usuario Teste" });
            dbContext.SaveChanges();
            return dbContext;
        }

        [Fact]
        public async Task ValidarReservaParaIoT_ReservaInvalida_RetornaStatusInvalido()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var controller = new ReservasController(dbContext); // Injete mocks se houver outras dependências

            // Act
            var result = await controller.ValidarReservaParaIoT("CODIGO-INVALIDO");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value;
            Assert.Equal("invalido", value.GetType().GetProperty("status").GetValue(value, null));
        }

        [Fact]
        public async Task PostReserva_ComVagas_CriaReservaERetornaCreatedAt()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var controller = new ReservasController(dbContext);
            // Mock UrlHelper para HATEOAS (simplificado)
            controller.Url = new Moq.Mock<IUrlHelper>().Object;


            var novaReserva = new Reserva
            {
                UsuarioId = 1,
                AbrigoId = 1,
                QuantidadePessoas = 2,
                UsouVagaCarro = true
            };

            // Act
            var result = await controller.PostReserva(novaReserva);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            //dynamic value = createdAtActionResult.Value;
            //var reservaCriada = Assert.IsType<Reserva>(value.reserva); // Ajuste para pegar a reserva do objeto anônimo
            var valObj = createdAtActionResult.Value;
            var reservaProp = valObj.GetType().GetProperty("reserva");
            Assert.NotNull(reservaProp);
            var reservaCriada = Assert.IsType<Reserva>(reservaProp.GetValue(valObj, null));

            Assert.NotNull(reservaCriada.CodigoReserva);
            Assert.Equal(8, dbContext.Abrigos.First(a => a.Id == 1).VagasPessoasDisponiveis); // 10 - 2
            Assert.Equal(1, dbContext.Abrigos.First(a => a.Id == 1).VagasCarrosDisponiveis); // 2 - 1
        }
    }
} 