namespace DB.Models;

public class BaseEntity
{
    Guid Id { get; set; }
    DateTime Created { get; set; }
    DateTime Modified { get; set; }
}