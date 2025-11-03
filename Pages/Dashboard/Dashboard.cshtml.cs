using Akwarium.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ScottPlot;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Akwarium.Pages.Dashboard
{
    public class DashboardModel : PageModel
    {
        private readonly AkwariumDbContext _context;

        public DashboardModel(AkwariumDbContext context)
        {
            _context = context;
        }

        public string ActiveTab { get; private set; } = "overview";
        public string UserName { get; private set; } = "";

        public List<SelectListItem> AquariumOptions { get; private set; } = new();
        public List<SelectListItem> SensorOptions { get; private set; } = new();

        [BindProperty(SupportsGet = true)] public int? AquariumId { get; set; }
        [BindProperty(SupportsGet = true)] public int? SensorId { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }

        // porównanie
        [BindProperty(SupportsGet = true)] public int? CompareSensor1Id { get; set; }
        [BindProperty(SupportsGet = true)] public int? CompareSensor2Id { get; set; }
        public bool CanShowCompareChart =>
            CompareSensor1Id.HasValue &&
            CompareSensor2Id.HasValue &&
            CompareSensor1Id.Value != CompareSensor2Id.Value;

        public List<SensorReadingVm> LatestReadings { get; private set; } = new();

        public List<ThresholdVm> Thresholds { get; private set; } = new();
        [BindProperty] public List<ThresholdInput> ThresholdInputs { get; set; } = new();

        [TempData] public string? ExportMessage { get; set; }

        public class SensorReadingVm
        {
            public int SensorId { get; set; }
            public string SensorName { get; set; } = "";
            public string SensorType { get; set; } = "";
            public double Value { get; set; }
            public DateTime TimeAdded { get; set; }
        }

        public class ThresholdVm
        {
            public int SensorId { get; set; }
            public string SensorName { get; set; } = "";
            public string SensorType { get; set; } = "";
            public double? LatestValue { get; set; }
            public double? Min { get; set; }
            public double? Max { get; set; }
            public bool IsOutOfRange { get; set; }
        }

        public class ThresholdInput
        {
            public int SensorId { get; set; }
            public string? Min { get; set; }   // ← było double?
            public string? Max { get; set; }
        }

        public class ThresholdRange
        {
            public double? Min { get; set; }
            public double? Max { get; set; }

            public bool IsOutOfRange(double value)
            {
                if (Min.HasValue && value < Min.Value) return true;
                if (Max.HasValue && value > Max.Value) return true;
                return false;
            }
        }



        private Dictionary<int, ThresholdRange> _currentThresholds = new();
        public async Task<IActionResult> OnGetAsync(string? tab, int? aquariumId)
        {
            UserName = HttpContext.Session.GetString("UserName") ?? "";
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                var returnUrl = Url.Page("/Dashboard/Dashboard", new { tab, AquariumId = aquariumId, SensorId, From, To });
                return RedirectToPage("/Account/Login", new { returnUrl });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return RedirectToPage("/Account/Login");

            ActiveTab = string.IsNullOrWhiteSpace(tab) ? "overview" : tab.ToLowerInvariant();
            if (aquariumId.HasValue)
                AquariumId = aquariumId;

            // --- AKWARIA ---
            AquariumOptions = await _context.Aquariums
                .Where(a => a.UserId == user.UserId)
                .OrderBy(a => a.AquariumName)
                .Select(a => new SelectListItem
                {
                    Value = a.AquariumId.ToString(),
                    Text = a.AquariumName
                })
                .ToListAsync();

            // --- CZUJNIKI ---
            var sensorsQuery = _context.Sensors
                .Include(s => s.Aquarium)
                .Where(s => s.Aquarium.UserId == user.UserId);

            if (AquariumId.HasValue)
                sensorsQuery = sensorsQuery.Where(s => s.AquariumId == AquariumId.Value);

            var sensorList = await sensorsQuery
                .OrderBy(s => s.SensorName)
                .ToListAsync();

            SensorOptions = sensorList
                .Select(s => new SelectListItem
                {
                    Value = s.SensorId.ToString(),
                    Text = $"{s.SensorName} ({s.SensorType})"
                })
                .ToList();

            // --- OSTATNIE ODCZYTY (kafelki + porównanie) ---
            LatestReadings = new();
            var sensorIds = sensorList.Select(s => s.SensorId).ToList();

            if (sensorIds.Any())
            {
                var data = await _context.SensorData
                    .Where(d => sensorIds.Contains(d.SensorId))
                    .OrderByDescending(d => d.TimeAdded)
                    .ToListAsync();

                LatestReadings = (
                    from d in data
                    group d by d.SensorId into g
                    let last = g.First()
                    join s in sensorList on g.Key equals s.SensorId
                    select new SensorReadingVm
                    {
                        SensorId = s.SensorId,
                        SensorName = s.SensorName,
                        SensorType = s.SensorType,
                        Value = last.Value,
                        TimeAdded = last.TimeAdded
                    }).ToList();
            }

            // --- PROGI z bazy -> słownik _currentThresholds ---
            _currentThresholds = new Dictionary<int, ThresholdRange>();

            if (sensorIds.Any())
            {
                var thrRows = await _context.SensorThresholds
                    .Where(t => t.UserId == user.UserId && sensorIds.Contains(t.SensorId))
                    .ToListAsync();

                foreach (var row in thrRows)
                {
                    _currentThresholds[row.SensorId] = new ThresholdRange
                    {
                        Min = row.MinValue,
                        Max = row.MaxValue
                    };
                }
            }

            // --- ViewModel progów do tabelki ---
            Thresholds = new List<ThresholdVm>();

            foreach (var s in sensorList)
            {
                double? latestValue = LatestReadings.FirstOrDefault(x => x.SensorId == s.SensorId)?.Value;
                _currentThresholds.TryGetValue(s.SensorId, out var range);

                Thresholds.Add(new ThresholdVm
                {
                    SensorId = s.SensorId,
                    SensorName = s.SensorName,
                    SensorType = s.SensorType,
                    LatestValue = latestValue,
                    Min = range?.Min,
                    Max = range?.Max,
                    IsOutOfRange = latestValue.HasValue && range?.IsOutOfRange(latestValue.Value) == true
                });
            }

            // --- zakres czasu dla wykresów ---
            From ??= DateTime.Now.AddDays(-1);
            To ??= DateTime.Now;
            if (From > To)
                (From, To) = (To, From);

            return Page();
        }


        // pomocnicza do kafelków
        public ThresholdRange? GetThresholdForSensor(int sensorId)
        {
            if (_currentThresholds.TryGetValue(sensorId, out var thr))
                return thr;
            return null;
        }


        // POST: zapis progów


        public async Task<IActionResult> OnPostThresholds(int? aquariumId)
        {

            

            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToPage("/Account/Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return RedirectToPage("/Account/Login");



            double? Parse(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return null;
                s = s.Trim().Replace(',', '.'); // akceptuj kropkę i przecinek
                return double.TryParse(s, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var d)
                    ? d
                    : (double?)null;
            }

            if (ThresholdInputs != null && ThresholdInputs.Count > 0)
            {
                var sensorIds = ThresholdInputs.Select(t => t.SensorId).ToList();

                // istniejące progi tego usera dla tych sensorów
                var existing = await _context.SensorThresholds
                    .Where(t => t.UserId == user.UserId && sensorIds.Contains(t.SensorId))
                    .ToListAsync();

                foreach (var input in ThresholdInputs)
                {
                    var min = Parse(input.Min);
                    var max = Parse(input.Max);

                    var row = existing.FirstOrDefault(e => e.SensorId == input.SensorId);

                    if (min == null && max == null)
                    {
                        // brak progów – usuń rekord jeśli był
                        if (row != null)
                            _context.SensorThresholds.Remove(row);
                    }
                    else
                    {
                        if (row == null)
                        {
                            row = new SensorThreshold
                            {
                                UserId = user.UserId,
                                SensorId = input.SensorId
                            };
                            _context.SensorThresholds.Add(row);
                            existing.Add(row);
                        }

                        row.MinValue = min;
                        row.MaxValue = max;
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Dashboard/Dashboard", new { tab = "thresholds", AquariumId = aquariumId });
        }



        // === wykres jednego sensora ===
        public async Task<IActionResult> OnGetChartAsync(int sensorId, DateTime? from, DateTime? to, int width = 1000, int height = 360)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var sensor = await _context.Sensors
                .Include(s => s.Aquarium).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(s => s.SensorId == sensorId && s.Aquarium.User.Email == email);

            if (sensor is null) return Forbid();

            var fromDt = from ?? DateTime.Now.AddDays(-1);
            var toDt = to ?? DateTime.Now;
            if (fromDt > toDt) (fromDt, toDt) = (toDt, fromDt);

            var points = await _context.SensorData
                .Where(d => d.SensorId == sensorId &&
                            d.TimeAdded >= fromDt &&
                            d.TimeAdded <= toDt)
                .OrderBy(d => d.TimeAdded)
                .Select(d => new { d.TimeAdded, d.Value })
                .ToListAsync();

            double[] xs = points.Select(p => p.TimeAdded.ToOADate()).ToArray();
            double[] ys = points.Select(p => (double)p.Value).ToArray();

            var plt = new ScottPlot.Plot(width, height);
            if (xs.Length > 0)
            {
                plt.AddScatter(xs, ys, lineWidth: 2);
                plt.XAxis.DateTimeFormat(true);
            }
            else
            {
                plt.SetAxisLimits(0, 1, 0, 1);
                var txt = plt.AddText("Brak danych w wybranym zakresie", 0.5, 0.5);
                txt.Font.Size = 16;
                txt.Alignment = Alignment.MiddleCenter;
                txt.Color = System.Drawing.Color.DimGray;
            }

            plt.Title($"{sensor.SensorName} — {sensor.SensorType}");
            plt.YLabel("Wartość");
            plt.XLabel("Czas");

            using var ms = new MemoryStream();
            using var bmp = plt.GetBitmap();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return File(ms.ToArray(), "image/png");
        }

        // === wykres porównawczy dwóch sensorów ===
        public async Task<IActionResult> OnGetCompareChartAsync(int sensor1Id, int sensor2Id, DateTime? from, DateTime? to, int width = 1000, int height = 360)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            if (sensor1Id == sensor2Id) return BadRequest();

            var sensors = await _context.Sensors
                .Include(s => s.Aquarium).ThenInclude(a => a.User)
                .Where(s => (s.SensorId == sensor1Id || s.SensorId == sensor2Id) &&
                            s.Aquarium.User.Email == email)
                .ToListAsync();

            if (sensors.Count != 2) return Forbid();

            var s1 = sensors.First(s => s.SensorId == sensor1Id);
            var s2 = sensors.First(s => s.SensorId == sensor2Id);

            var fromDt = from ?? DateTime.Now.AddDays(-1);
            var toDt = to ?? DateTime.Now;
            if (fromDt > toDt) (fromDt, toDt) = (toDt, fromDt);

            var data1 = await _context.SensorData
                .Where(d => d.SensorId == sensor1Id &&
                            d.TimeAdded >= fromDt &&
                            d.TimeAdded <= toDt)
                .OrderBy(d => d.TimeAdded)
                .ToListAsync();

            var data2 = await _context.SensorData
                .Where(d => d.SensorId == sensor2Id &&
                            d.TimeAdded >= fromDt &&
                            d.TimeAdded <= toDt)
                .OrderBy(d => d.TimeAdded)
                .ToListAsync();

            var plt = new ScottPlot.Plot(width, height);

            if (data1.Any())
            {
                var xs = data1.Select(p => p.TimeAdded.ToOADate()).ToArray();
                var ys = data1.Select(p => (double)p.Value).ToArray();
                plt.AddScatter(xs, ys, lineWidth: 2, label: s1.SensorName);
            }

            if (data2.Any())
            {
                var xs = data2.Select(p => p.TimeAdded.ToOADate()).ToArray();
                var ys = data2.Select(p => (double)p.Value).ToArray();
                plt.AddScatter(xs, ys, lineWidth: 2, label: s2.SensorName);
            }

            if (!data1.Any() && !data2.Any())
            {
                plt.SetAxisLimits(0, 1, 0, 1);
                var txt = plt.AddText("Brak danych w wybranym zakresie", 0.5, 0.5);
                txt.Font.Size = 16;
                txt.Alignment = Alignment.MiddleCenter;
                txt.Color = System.Drawing.Color.DimGray;
            }

            plt.Legend();
            plt.XAxis.DateTimeFormat(true);
            plt.Title("Porównanie czujników");
            plt.YLabel("Wartość");
            plt.XLabel("Czas");

            using var ms = new MemoryStream();
            using var bmp = plt.GetBitmap();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return File(ms.ToArray(), "image/png");
        }

        // === CSV / ZIP eksport ===
        public async Task<IActionResult> OnGetCsvAsync(
            int sensorId,
            DateTime? from,
            DateTime? to,
            int? aquariumId,
            string? period,
            char delim = ';',
            bool excelFriendly = true)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var now = DateTime.Now;
            DateTime fromDt;
            DateTime toDt;

            switch (period?.ToLowerInvariant())
            {
                case "week":
                    var endOfYesterday = now.Date;
                    toDt = endOfYesterday;
                    fromDt = endOfYesterday.AddDays(-7);
                    break;

                case "month":
                    var endOfYesterdayM = now.Date;
                    toDt = endOfYesterdayM;
                    fromDt = endOfYesterdayM.AddDays(-30);
                    break;

                case "day":
                    var day = (from ?? now).Date;
                    fromDt = day;
                    toDt = day.AddDays(1);
                    break;

                default:
                    fromDt = from ?? now.AddDays(-7);
                    toDt = to ?? now;
                    break;
            }

            if (fromDt > toDt)
                (fromDt, toDt) = (toDt, fromDt);

            var culture = new CultureInfo("pl-PL");

            // 0 = wszystkie sensory -> ZIP
            if (sensorId == 0)
            {
                var sensorsQuery = _context.Sensors
                    .Include(s => s.Aquarium).ThenInclude(a => a.User)
                    .Where(s => s.Aquarium.User.Email == email);

                if (aquariumId.HasValue)
                    sensorsQuery = sensorsQuery.Where(s => s.AquariumId == aquariumId.Value);

                var sensors = await sensorsQuery
                    .OrderBy(s => s.SensorName)
                    .ToListAsync();

                if (sensors.Count == 0)
                {
                    ExportMessage = "Brak czujników do eksportu.";
                    return RedirectToPage("/Dashboard/Dashboard", new { tab = "export", AquariumId = aquariumId });
                }

                using var zipMs = new MemoryStream();
                bool anyData = false;

                using (var zip = new ZipArchive(zipMs, ZipArchiveMode.Create, true))
                {
                    foreach (var s in sensors)
                    {
                        var rows = await _context.SensorData
                            .Where(d => d.SensorId == s.SensorId &&
                                        d.TimeAdded >= fromDt &&
                                        d.TimeAdded <= toDt)
                            .OrderBy(d => d.TimeAdded)
                            .ToListAsync();

                        if (rows.Count == 0)
                            continue;

                        anyData = true;

                        var sb = new StringBuilder();

                        if (excelFriendly && delim == ';')
                            sb.AppendLine("sep=;");

                        sb.AppendLine($"SensorName{delim}SensorType{delim}TimeAdded{delim}Value");

                        foreach (var r in rows)
                            sb.AppendLine($"{s.SensorName}{delim}{s.SensorType}{delim}{r.TimeAdded:O}{delim}{r.Value.ToString(culture)}");

                        var entryName = $"{Slug(s.SensorName)}_{fromDt:yyyyMMddHHmm}-{toDt:yyyyMMddHHmm}.csv";
                        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);

                        using var es = entry.Open();
                        var buf = new UTF8Encoding(true).GetBytes(sb.ToString());
                        await es.WriteAsync(buf, 0, buf.Length);
                    }
                }

                if (!anyData)
                {
                    ExportMessage = "Brak danych w wybranym okresie dla wybranych filtrów.";
                    return RedirectToPage("/Dashboard/Dashboard", new { tab = "export", AquariumId = aquariumId });
                }

                var zipBytes = zipMs.ToArray();
                var zipName = $"export_all_{fromDt:yyyyMMddHHmm}-{toDt:yyyyMMddHHmm}.zip";
                return File(zipBytes, "application/zip", zipName);
            }

            // pojedynczy sensor -> CSV
            var sensor = await _context.Sensors
                .Include(s => s.Aquarium).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(s => s.SensorId == sensorId &&
                                          s.Aquarium.User.Email == email);

            if (sensor is null)
            {
                ExportMessage = "Nie znaleziono wskazanego czujnika.";
                return RedirectToPage("/Dashboard/Dashboard", new { tab = "export", AquariumId = aquariumId });
            }

            var data = await _context.SensorData
                .Where(d => d.SensorId == sensorId &&
                            d.TimeAdded >= fromDt &&
                            d.TimeAdded <= toDt)
                .OrderBy(d => d.TimeAdded)
                .ToListAsync();

            if (data.Count == 0)
            {
                ExportMessage = "Brak danych w wybranym okresie dla tego czujnika.";
                return RedirectToPage("/Dashboard/Dashboard", new { tab = "export", AquariumId = aquariumId });
            }

            var csv = new StringBuilder();

            if (excelFriendly && delim == ';')
                csv.AppendLine("sep=;");

            csv.AppendLine($"SensorName{delim}SensorType{delim}TimeAdded{delim}Value");

            foreach (var r in data)
                csv.AppendLine($"{sensor.SensorName}{delim}{sensor.SensorType}{delim}{r.TimeAdded:O}{delim}{r.Value.ToString(culture)}");

            var bytes = new UTF8Encoding(true).GetBytes(csv.ToString());
            var fileName = $"export_{Slug(sensor.SensorName)}_{fromDt:yyyyMMddHHmm}-{toDt:yyyyMMddHHmm}.csv";
            return File(bytes, "text/csv", fileName);
        }

        // KONFIGURACJA – dodanie akwarium
        public async Task<IActionResult> OnPostAddAquariumAsync(string AquariumName)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToPage("/Account/Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return RedirectToPage("/Account/Login");

            var aq = new Aquarium
            {
                AquariumName = AquariumName,
                UserId = user.UserId
            };

            _context.Aquariums.Add(aq);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Dashboard/Dashboard", new { tab = "config" });
        }

        // KONFIGURACJA – dodanie czujnika
        public async Task<IActionResult> OnPostAddSensorAsync(int AquariumId, string SensorName, string SensorType, string? Description)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToPage("/Account/Login");

            var aquarium = await _context.Aquariums
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AquariumId == AquariumId && a.User.Email == email);

            if (aquarium == null)
                return RedirectToPage("/Dashboard/Dashboard", new { tab = "config" });

            var sensor = new Sensor
            {
                AquariumId = AquariumId,
                SensorName = SensorName,
                SensorType = SensorType,
                Description = Description
            };

            _context.Sensors.Add(sensor);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Dashboard/Dashboard", new { tab = "config", AquariumId });
        }

        private static string Slug(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(clean) ? "sensor" : clean;
        }
    }
}
