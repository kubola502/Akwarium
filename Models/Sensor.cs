using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Akwarium.Models;

public partial class Sensor
{
    [Key]
    [Column("SensorID")]
    public int SensorId { get; set; }

    [StringLength(100)]
    public string SensorName { get; set; } = null!;

    [StringLength(50)]
    public string SensorType { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    [Column("AquariumID")]
    public int AquariumId { get; set; }

    [ForeignKey("AquariumId")]
    [InverseProperty("Sensors")]
    public virtual Aquarium Aquarium { get; set; } = null!;

    [InverseProperty("Sensor")]
    public virtual ICollection<SensorData> SensorData { get; set; } = new List<SensorData>();

    public double? MinValue { get; set; }

    public double? MaxValue { get; set; }

}
