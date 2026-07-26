using Microsoft.EntityFrameworkCore;
using Sircip.Server.Models;

namespace Sircip.Server.Data;

public class SircipDbContext : DbContext
{
    public SircipDbContext(DbContextOptions<SircipDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.Property(u => u.NombreUsuario).HasMaxLength(100).IsRequired();
            entity.HasIndex(u => u.NombreUsuario).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20).IsRequired();
        });
    }
}
