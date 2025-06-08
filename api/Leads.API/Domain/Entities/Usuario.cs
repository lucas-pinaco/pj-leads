namespace Leads.API.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string NomeUsuario { get; set; }  // pode ser e-mail também
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public bool Ativo { get; set; } = true;
        public string Perfil { get; set; }

        // Relacionamento com Cliente
        public int? ClienteId { get; set; }
        public virtual Cliente Cliente { get; set; }

        // Relacionamento com Histórico
        public virtual ICollection<HistoricoExportacao> Exportacoes { get; set; } = new List<HistoricoExportacao>();
    }
}