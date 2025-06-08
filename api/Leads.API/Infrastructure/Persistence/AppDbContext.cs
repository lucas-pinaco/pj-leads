using Leads.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Lead> Leads { get; set; }
    public DbSet<Checkout> Checkouts { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Arquivo> Arquivos { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Plano> Planos { get; set; }
    public DbSet<HistoricoExportacao> HistoricoExportacoes { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new LeadConfiguration());

        modelBuilder.Entity<Checkout>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmailCliente).HasMaxLength(200);
        });
    }

}