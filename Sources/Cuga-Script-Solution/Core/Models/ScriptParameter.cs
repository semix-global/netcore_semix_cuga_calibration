using CugaCalibration.ViewModels.Common;
using Microsoft.Extensions.Logging;

namespace CugaScript.Core.Models;

public sealed class ScriptParameter
{
    public required AdsViewModel AdsViewModel { get; set; }
    public required AfViewModel AfViewModel { get; set; }
    public required LaserViewModel LaserViewModel { get; set; }
    public required MicroscopeViewModel MicroscopeViewModel { get; set; }
    public required ReviewViewModel ReviewViewModel { get; set; }
    public required StageViewModel StageViewModel { get; set; }
    public required ILogger<ScriptParameter> Logger { get; set; }
    public CancellationToken CancellationToken { get; set; }
}