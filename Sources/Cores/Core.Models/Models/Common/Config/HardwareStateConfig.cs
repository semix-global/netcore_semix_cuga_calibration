using Core.Models.Enums.HardwareType;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Common.Config;

public sealed class HardwareStateConfig(
    IDictionary<HardwareMotorTypeEnum, HardwareStateDTO> motors,
    IDictionary<HardwareFourierTypeEnum, HardwareStateDTO> fouriers,
    IDictionary<HardwareClinderTypeEnum, HardwareStateDTO> clinders)
{
    public static readonly HardwareStateConfig Default = new(
        new Dictionary<HardwareMotorTypeEnum, HardwareStateDTO>(),
        new Dictionary<HardwareFourierTypeEnum, HardwareStateDTO>(),
        new Dictionary<HardwareClinderTypeEnum, HardwareStateDTO>());

    public IReadOnlyDictionary<HardwareMotorTypeEnum, HardwareStateDTO> MotorHardwares { get; } = new ReadOnlyDictionary<HardwareMotorTypeEnum, HardwareStateDTO>(
        new Dictionary<HardwareMotorTypeEnum, HardwareStateDTO>(motors));

    public IReadOnlyDictionary<HardwareFourierTypeEnum, HardwareStateDTO> FourierHardwares { get; } = new ReadOnlyDictionary<HardwareFourierTypeEnum, HardwareStateDTO>(
        new Dictionary<HardwareFourierTypeEnum, HardwareStateDTO>(fouriers));

    public IReadOnlyDictionary<HardwareClinderTypeEnum, HardwareStateDTO> ClinderHardwares { get; } = new ReadOnlyDictionary<HardwareClinderTypeEnum, HardwareStateDTO>(
        new Dictionary<HardwareClinderTypeEnum, HardwareStateDTO>(clinders));

}

public record HardwareStateDTO(bool Enabled);