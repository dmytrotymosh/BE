using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DB.Models
{
    public class Message :BaseEntity
    {
        [Required]
        [Column("chat_id", TypeName = "uuid")]
        public Guid ChatId { get; set; }
        [ForeignKey("ChatId")]
        public Chat Chat { get; set; }
        [Required]
        [Column("sender_id", TypeName = "uuid")]
        public Guid SenderId { get; set; }
        [ForeignKey("SenderId")]
        public User Sender { get; set; }
        [Required]
        [MaxLength(1000)]
        [Column("text", TypeName = "varchar(1000)")]
        public string Text { get; set; }
        [Column("sent_at")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
