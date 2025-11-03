namespace Akwarium.Models
{
    public partial class SensorThreshold
    {
        public int SensorThresholdId { get; set; }

        public int UserId { get; set; }

        public int SensorId { get; set; }

        public double? MinValue { get; set; }

        public double? MaxValue { get; set; }
    }
}
