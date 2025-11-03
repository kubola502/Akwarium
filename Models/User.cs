using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Akwarium.Models;

[Index("Email", Name = "UQ__Users__A9D10534CBCD7732", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(50)]
    public string Nick { get; set; } = null!;

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string Password { get; set; } = null!;

    public string Role { get; set; } = string.Empty!;

    [InverseProperty("User")]
    public virtual ICollection<Aquarium> Aquaria { get; set; } = new List<Aquarium>();
}
