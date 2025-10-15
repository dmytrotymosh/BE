using Microsoft.EntityFrameworkCore;
using BookLoop.Data.Models;

namespace DB;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Exchange> Exchanges { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasOne(b => b.Owner)
                  .WithMany(u => u.Books)
                  .HasForeignKey(b => b.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Exchange>(entity =>
        {
            entity.HasOne(e => e.Book)
                  .WithMany()
                  .HasForeignKey(e => e.BookId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Owner)
                  .WithMany()
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Receiver)
                  .WithMany()
                  .HasForeignKey(e => e.ReceiverId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasOne(c => c.User1)
                  .WithMany()
                  .HasForeignKey(c => c.UserId1)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.User2)
                  .WithMany()
                  .HasForeignKey(c => c.UserId2)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasOne(m => m.Chat)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(m => m.ChatId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.Sender)
                  .WithMany()
                  .HasForeignKey(m => m.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}