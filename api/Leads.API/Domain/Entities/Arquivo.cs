using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Leads.API.Domain.Entities
{
    [Table("Arquivos")]
    public class Arquivo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Nome { get; set; }

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; }

        [Required]
        public byte[] Data { get; set; }

        [Required]
        public DateTime DataUpload { get; set; } = DateTime.UtcNow;

        // Novos campos para controle de processamento
        public DateTime? ProcessadoEm { get; set; }

        public int? QuantidadeLeads { get; set; }

        [StringLength(500)]
        public string? ErroProcessamento { get; set; }
    }
}
