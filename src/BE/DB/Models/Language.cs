using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DB.Models;

public class Language : BaseEntity
{
    [Required]
    [MaxLength(50)]
    [Column("name", TypeName = "varchar(50)")]
    public string Name { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("code", TypeName = "varchar(10)")]
    public string Code { get; set; }
}