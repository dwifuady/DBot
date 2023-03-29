using Microsoft.EntityFrameworkCore;

namespace DBot.Console.Entities;

public class DbotContext : DbContext
{
    public DbotContext(DbContextOptions<DbotContext> options) : base(options)
    {
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "DBot");
    }
    public DbSet<Conversation>? Conversations { get; set; }
}
