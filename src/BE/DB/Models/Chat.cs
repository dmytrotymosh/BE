using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace BookLoop.Data.Models
{
    public class Chat
    {
        [Key]
        [Column("id", TypeName = "uuid")]
        public Guid Id { get; set; } = Guid.NewGuid();
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