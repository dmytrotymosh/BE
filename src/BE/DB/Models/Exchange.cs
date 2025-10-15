using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookLoop.Data.Models.Enums;

namespace DB.Models
{
    public class Exchange :BaseEntity
    {
        [Required]
        [Column("book_id", TypeName = "uuid")]
        public Guid BookId { get; set; }
        [ForeignKey("BookId")]
        public Book Book { get; set; }
        [Required]
        [Column("owner_id", TypeName = "uuid")]
        public Guid OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public User Owner { get; set; }
        [Required]
        [Column("receiver_id", TypeName = "uuid")]
        public Guid ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }
        [Required]
        [Column("status", TypeName = "integer")]
        public ExchangeStatus Status { get; set; }
        [Required]
        [Column("rating", TypeName = "integer")]
        public int Rating { get; set; }
        [MaxLength(500)]
        [Column("comment", TypeName = "varchar(500)")]
        public string Comment { get; set; }
    }
}
