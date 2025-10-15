using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookLoop.Data.Models
{
    public class Book
    {
        [Key]
        [Column("id", TypeName = "uuid")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [Column("owner_id", TypeName = "uuid")]
        public Guid OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public User Owner { get; set; }
        [Required]
        [MaxLength(100)]
        [Column("title", TypeName = "varchar(100)")]
        public string Title { get; set; }
        [Required]
        [MaxLength(500)]
        [Column("description", TypeName = "varchar(500)")]
        public string Description { get; set; }
        [Required]
        [MaxLength(100)]
        [Column("state", TypeName = "varchar(100)")]
        public string State { get; set; }
        [Required]
        [MaxLength(50)]
        [Column("genre", TypeName = "varchar(50)")]
        public string Genre { get; set; }
    }
}