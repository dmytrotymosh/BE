using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DB.Models
{
    public class Chat :BaseEntity
    {
        [Required]
        [Column("user_id_1", TypeName = "uuid")]
        public Guid UserId1 { get; set; }
        [ForeignKey("UserId1")]
        public User User1 { get; set; }
        [Required]
        [Column("user_id_2", TypeName = "uuid")]
        public Guid UserId2 { get; set; }
        [ForeignKey("UserId2")]
        public User User2 { get; set; }
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}