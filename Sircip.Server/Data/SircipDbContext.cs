using Microsoft.EntityFrameworkCore;
using Sircip.Server.Models;
using Sircip.Shared.Models;

namespace Sircip.Server.Data;

public class SircipDbContext : DbContext
{
    public SircipDbContext(DbContextOptions<SircipDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Importacion> Importaciones => Set<Importacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.Property(u => u.NombreUsuario).HasMaxLength(100).IsRequired();
            entity.HasIndex(u => u.NombreUsuario).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Importacion>(entity =>
        {
            entity.Property(i => i.Periodo).IsRequired();
            entity.Property(i => i.FechaImportacionUtc).IsRequired();
            entity.Property(i => i.CantidadRegistros).IsRequired();
            entity.Property(i => i.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(i => i.Error).HasMaxLength(2000);

            entity.HasOne(i => i.Usuario)
                .WithMany()
                .HasForeignKey(i => i.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Un período puede tener muchas importaciones fallidas o borradas en
            // el historial, pero una sola vigente: para reimportarlo hay que
            // eliminarlo antes, y ahí deja de estar en estado Exitosa.
            entity.HasIndex(i => i.Periodo)
                .IsUnique()
                .HasFilter($"[{nameof(Importacion.Estado)}] = '{nameof(EstadoImportacion.Exitosa)}'");
        });
    }
}
