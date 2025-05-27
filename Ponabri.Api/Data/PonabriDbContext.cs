using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Models; // Ajuste o namespace se necessário

namespace Ponabri.Api.Data
{
    public class PonabriDbContext : DbContext
    {
        public PonabriDbContext(DbContextOptions<PonabriDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Abrigo> Abrigos { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurações adicionais do modelo, chaves compostas, etc.
            // Exemplo: Código da reserva deve ser único
            modelBuilder.Entity<Reserva>()
                .HasIndex(r => r.CodigoReserva)
                .IsUnique();

            // Relacionamentos (EF Core geralmente descobre, mas pode ser explícito)
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.UsuarioId);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Abrigo)
                .WithMany(a => a.Reservas)
                .HasForeignKey(r => r.AbrigoId);
        }
    }
} 