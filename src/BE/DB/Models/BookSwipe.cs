using System.ComponentModel.DataAnnotations.Schema;
using DB.Models.Enums;

namespace DB.Models;

public class BookSwipe : BaseEntity
{
    [Column("user_id", TypeName = "uuid")]
    public Guid UserId { get; set; }
    public User User { get; set; }

    [Column("book_id", TypeName = "uuid")]
    public Guid BookId { get; set; }
    public Book Book { get; set; }

    [Column("swipe_type", TypeName = "integer")]
    public SwipeType SwipeType { get; set; }
}
