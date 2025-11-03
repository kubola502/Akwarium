using Akwarium.Models;
using Microsoft.AspNetCore.Mvc;
namespace Akwarium.Controllers


{
    [ApiController]
    [Route("/api/[controller]")]
    public class SensorsDataController : ControllerBase
    {
        private readonly AkwariumDbContext _context;
        public SensorsDataController(AkwariumDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostSensorData([FromBody] SensorData sensorData)
        {
            if (sensorData == null)
                return BadRequest("Brak danych");

            sensorData.TimeAdded = DateTime.Now; // jeśli ESP nie wysyła czasu
            _context.SensorData.Add(sensorData);
            await _context.SaveChangesAsync();

            return Ok("Dane zapisane");
        }

        [HttpGet("{SensorID}")]

        public IActionResult GetSensorData(int sensorID)
        {
            var data = _context.SensorData
                .Where(s => s.SensorId == sensorID)
                .OrderByDescending(s => s.TimeAdded)
                .Take(10)
                .ToList();

            return Ok(data);
        }

    }
}