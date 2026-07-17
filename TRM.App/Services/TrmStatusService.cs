using System.Text.Json;
using TRM.App.Models;

namespace TRM.App.Services;

/// <summary>
/// Loads TRM project status from wwwroot/data/trm-v*-status.json.
/// Supports version navigation (first/back/forward/newest).
/// Returns null when JSON is unavailable — caller must handle missing data.
/// Never returns stale/default values.
/// </summary>
public sealed class TrmStatusService
{
    private readonly IWebHostEnvironment _env;
    private TrmStatusModel? _cached;
    private bool _loadAttempted;
    private string? _currentVersion;
    private List<string>? _availableVersions;

    public TrmStatusService(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Returns all available version suffixes found in the data directory.
    /// </summary>
    public List<string> GetAvailableVersions()
    {
        if (_availableVersions is not null) return _availableVersions;

        var dir = Path.Combine(_env.WebRootPath, "data");
        if (!Directory.Exists(dir)) { _availableVersions = new List<string>(); return _availableVersions; }

        _availableVersions = Directory.GetFiles(dir, "*.json")
            .Where(f => Path.GetFileName(f).EndsWith("-status.json"))
            .Select(f => Path.GetFileNameWithoutExtension(f).Replace("trm-v", "").Replace("-status", ""))
            .OrderBy(v => {
                var parts = v.Split('-', '.');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int maj) &&
                    int.TryParse(parts[1], out int min))
                    return maj * 100 + min;
                return 9999;
            })
            .ToList();
        return _availableVersions;
    }

    public string? GetCurrentVersion() => _currentVersion;

    /// <summary>
    /// Loads the newest available status file.
    /// </summary>
    public async Task<TrmStatusModel?> GetStatusAsync()
    {
        var versions = GetAvailableVersions();
        if (versions.Count == 0) return null;
        var newest = versions.Last();
        return await LoadVersionAsync(newest);
    }

    /// <summary>
    /// Loads a specific version by suffix (e.g. "v5-10").
    /// </summary>
    public async Task<TrmStatusModel?> LoadVersionAsync(string version)
    {
        _currentVersion = version;
        _loadAttempted = false;
        _cached = null;

        try
        {
            var path = Path.Combine(_env.WebRootPath, "data", $"trm-v{version}-status.json");
            if (!File.Exists(path)) return null;

            await using var stream = File.OpenRead(path);
            var model = await JsonSerializer.DeserializeAsync<TrmStatusModel>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (model?.TestSummary is null || model.TestSummary.Total == 0)
                return null;

            _cached = model;
            _loadAttempted = true;
            return _cached;
        }
        catch
        {
            return null;
        }
    }

    public string? GetPreviousVersion()
    {
        var versions = GetAvailableVersions();
        if (versions.Count == 0 || _currentVersion is null) return null;
        var idx = versions.IndexOf(_currentVersion);
        return idx > 0 ? versions[idx - 1] : null;
    }

    public string? GetNextVersion()
    {
        var versions = GetAvailableVersions();
        if (versions.Count == 0 || _currentVersion is null) return null;
        var idx = versions.IndexOf(_currentVersion);
        return idx >= 0 && idx < versions.Count - 1 ? versions[idx + 1] : null;
    }

    public string? GetNewestVersion() => GetAvailableVersions().LastOrDefault();
    public string? GetFirstVersion() => GetAvailableVersions().FirstOrDefault();
}
