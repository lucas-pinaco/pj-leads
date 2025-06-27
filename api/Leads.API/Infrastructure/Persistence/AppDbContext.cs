using Leads.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

        // Configurações específicas para PostgreSQL

        // Configurar nomes de tabelas em minúsculo (convenção PostgreSQL)
        modelBuilder.Entity<Lead>().ToTable("leads");
        modelBuilder.Entity<Checkout>().ToTable("checkouts");
        modelBuilder.Entity<Usuario>().ToTable("usuarios");
        modelBuilder.Entity<Arquivo>().ToTable("arquivos");
        modelBuilder.Entity<Cliente>().ToTable("clientes");
        modelBuilder.Entity<Plano>().ToTable("planos");
        modelBuilder.Entity<HistoricoExportacao>().ToTable("historico_exportacoes");

        // Configurar relacionamentos
        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Cliente)
            .WithMany(c => c.Usuarios)
            .HasForeignKey(u => u.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Cliente>()
            .HasOne(c => c.Plano)
            .WithMany(p => p.Clientes)
            .HasForeignKey(c => c.PlanoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistoricoExportacao>()
            .HasOne(h => h.Cliente)
            .WithMany(c => c.Exportacoes)
            .HasForeignKey(h => h.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HistoricoExportacao>()
            .HasOne(h => h.Usuario)
            .WithMany(u => u.Exportacoes)
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configurar campos de texto grandes para PostgreSQL
        modelBuilder.Entity<HistoricoExportacao>()
            .Property(h => h.FiltrosUtilizados)
            .HasColumnType("text");

        // Configurar campos decimais
        modelBuilder.Entity<Plano>()
            .Property(p => p.Valor)
            .HasColumnType("decimal(10,2)");

        // Configurar campos de data com timezone
        modelBuilder.Entity<Lead>()
            .Property(l => l.DataAbertura)
            .HasColumnType("timestamp with time zone");

        modelBuilder.Entity<Usuario>()
            .Property(u => u.DataCriacao)
            .HasColumnType("timestamp with time zone");

        modelBuilder.Entity<Cliente>()
            .Property(c => c.DataCadastro)
            .HasColumnType("timestamp with time zone");

        modelBuilder.Entity<Cliente>()
            .Property(c => c.DataVencimento)
            .HasColumnType("timestamp with time zone");

        modelBuilder.Entity<Cliente>()
            .Property(c => c.DataUltimoPagamento)
            .HasColumnType("timestamp with time zone");

        modelBuilder.Entity<HistoricoExportacao>()
            .Property(h => h.DataExportacao)
            .HasColumnType("timestamp with time zone");

        modelBuilder.Entity<Arquivo>()
            .Property(a => a.DataUpload)
            .HasColumnType("timestamp with time zone");

        modelBuilder.Entity<Arquivo>()
            .Property(a => a.ProcessadoEm)
            .HasColumnType("timestamp with time zone");

        // Índices para melhor performance
        modelBuilder.Entity<Lead>()
            .HasIndex(l => l.CNPJ)
            .HasDatabaseName("ix_leads_cnpj");

        modelBuilder.Entity<Lead>()
            .HasIndex(l => l.ContatoEmail)
            .HasDatabaseName("ix_leads_email");

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ix_usuarios_email");

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.CNPJ)
            .IsUnique()
            .HasDatabaseName("ix_clientes_cnpj");
    }
}