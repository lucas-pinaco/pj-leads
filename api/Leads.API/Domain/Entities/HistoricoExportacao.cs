using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Leads.API.Domain.Entities
{
    [Table("HistoricoExportacoes")]
    public class HistoricoExportacao
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }
        public virtual Cliente Cliente { get; set; }

        [Required]
        public int UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; }

        [Required]
        public DateTime DataExportacao { get; set; } = DateTime.UtcNow;

        [Required]
        public int QuantidadeLeads { get; set; }

        [StringLength(500)]
        public string FiltrosUtilizados { get; set; } // JSON com os filtros aplicados

        [StringLength(255)]
        public string NomeArquivo { get; set; }

        [StringLength(200)]
        public string EmailDestino { get; set; }

        public bool EnviadoPorEmail { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Concluido"; // Concluido, Erro, Processando

        [StringLength(500)]
        public string MensagemErro { get; set; }

        // Informações do plano no momento da exportação
        [StringLength(100)]
        public string PlanoNome { get; set; }
        public int LimiteDisponivel { get; set; }
    }
}