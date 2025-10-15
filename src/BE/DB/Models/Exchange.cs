using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookLoop.Data.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookLoop.Data.Models
{
    public class Exchange
    {
        [Key]
        [Column("id", TypeName = "uuid")]
        public Guid Id { get; set; } = Guid.NewGuid();
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
