using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Leads.API.Domain.Entities
{
    [Table("Planos")]
    public class Plano
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(500)]
        public string Descricao { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Valor { get; set; }

        [Required]
        public int LimiteExportacoesMes { get; set; }

        [Required]
        public int LimiteLeadsPorExportacao { get; set; }

        // Recursos do Plano
        public bool PermiteExportarEmail { get; set; } = true;
        public bool PermiteExportarTelefone { get; set; } = true;
        public bool PermiteFiltrosAvancados { get; set; } = true;
        public bool SuportePrioritario { get; set; } = false;

        // Controle
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public int OrdemExibicao { get; set; } = 0;

        // Relacionamento com Clientes
        public virtual ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
    }
}