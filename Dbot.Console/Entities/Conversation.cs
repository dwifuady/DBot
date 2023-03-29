using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBot.Console.Entities;

public class Conversation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? InitialCommand { get; set; }
    public string? Message { get; set; }
    public string? MessageId { get; set; }
    public string? ParentId { get; set; }
    public string? OriginalMessageId { get; set; }
    public bool IsFromBot { get; set; }
}
