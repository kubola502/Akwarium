using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Akwarium.Models;

public partial class Aquarium
{
    [Key]
    [Column("AquariumID")]
    public int AquariumId { get; set; }

    [StringLength(100)]
    public string AquariumName { get; set; } = null!;

    [Column("UserID")]
    public int UserId { get; set; }

    [InverseProperty("Aquarium")]
    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();

    [ForeignKey("UserId")]
    [InverseProperty("Aquaria")]
    public virtual User User { get; set; } = null!;
}
