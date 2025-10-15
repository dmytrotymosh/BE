using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookLoop.Data.Models
{
    public class User
    {
        [Key]
        [Column("id", TypeName = "uuid")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [MaxLength(50)]
        [Column("first_name", TypeName = "varchar(50)")]
        public string FirstName { get; set; }
        [Required]
        [MaxLength(50)]
        [Column("last_name", TypeName = "varchar(50)")]
        public string LastName { get; set; }
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        [Column("email", TypeName = "varchar(100)")]
        public string Email { get; set; }
        [Required]
        [Column("password_hash", TypeName = "text")]
        public string PasswordHash { get; set; }
        [MaxLength(50)]
        [Column("timezone", TypeName = "varchar(50)")]
        public string TimeZone { get; set; } = "";
        [MaxLength(100)]
        [Column("location", TypeName = "varchar(100)")]
        public string Location { get; set; } = "";
        [Column("image", TypeName = "text")]
        public string Img { get; set; } = "";
        [MaxLength(500)]
        [Column("description", TypeName = "varchar(500)")]
        public string Description { get; set; } = "";
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}