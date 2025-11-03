using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Akwarium.Models;

[Index("SensorId", "TimeAdded", Name = "IX_SensorData_SensorID_TimeAdded")]
public partial class SensorData
{
    [Key]
    [Column("SensorDataID")]
    public int SensorDataId { get; set; }

    [Column("SensorID")]
    public int SensorId { get; set; }

    public double Value { get; set; }

    public DateTime TimeAdded { get; set; }

    [ForeignKey("SensorId")]
    [InverseProperty("SensorData")]
    public virtual Sensor? Sensor { get; set; } = null!;
}
