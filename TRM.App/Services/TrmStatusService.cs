using System.Text.Json;
using TRM.App.Models;

namespace TRM.App.Services;

/// <summary>
/// Loads TRM V4.1 project status from wwwroot/data/trm-v4-1-status.json.
/// Returns null when JSON is unavailable — caller must handle missing data.
/// Never returns stale/default values.
/// </summary>
public sealed class TrmStatusService
{
    private readonly IWebHostEnvironment _env;
    private TrmStatusModel? _cached;
    private bool _loadAttempted;

    public TrmStatusService(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Returns the deserialized status model, or null if the JSON file
    /// is missing, malformed, or cannot be read.
    /// Never returns a stale default.
    /// </summary>
    public async Task<TrmStatusModel?> GetStatusAsync()
    {
        if (_loadAttempted) return _cached;
        _loadAttempted = true;

        try
        {
            var path = Path.Combine(_env.WebRootPath, "data", "trm-v5-4-status.json");
            if (!File.Exists(path)) return null;

            await using var stream = File.OpenRead(path);
            var model = await JsonSerializer.DeserializeAsync<TrmStatusModel>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (model?.TestSummary is null || model.TestSummary.Total == 0)
                return null; // Reject malformed/empty data

            _cached = model;
            return _cached;
        }
        catch
        {
            return null;
        }
    }
}
