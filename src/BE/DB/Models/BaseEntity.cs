using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DB.Models;

public class BaseEntity
{
    [Key]
    [Column("id", TypeName = "uuid")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("created", TypeName = "timestamp")]
    public DateTime Created { get; set; }

    [Column("modified", TypeName = "timestamp")]
    public DateTime Modified { get; set; }
}