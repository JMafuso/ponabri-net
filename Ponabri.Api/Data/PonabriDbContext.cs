using Microsoft.EntityFrameworkCore;
using Ponabri.Api.Models; 

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
            modelBuilder.Entity<Reserva>()
                .HasIndex(r => r.CodigoReserva)
                .IsUnique();

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.UsuarioId);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Abrigo)
                .WithMany(a => a.Reservas)
                .HasForeignKey(r => r.AbrigoId);

            modelBuilder.Entity<Reserva>()
                .Property(r => r.UsouVagaCarro)
                .HasColumnType("bit");
        }
    }
}
