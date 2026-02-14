using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("Accounts")]
    public class Account
    {
        [Key] public int Id { get; init; }

        [Required] [StringLength(50)] public required string Username { get; init; } = null!;

        [Required] [StringLength(60)] public required string PasswordHash { get; init; } = null!;

        [Required] [StringLength(20)] public required string Role { get; init; } = null!;

        [StringLength(100)] public string? DisplayName { get; init; }

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
}