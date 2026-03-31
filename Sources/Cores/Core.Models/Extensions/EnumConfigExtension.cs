using Core.Models.Enums.HardwareType;
using Cuga.Data.DataStruct.Optics;

namespace Core.Models.Extensions;

public static class EnumConfigExtension
{
#if NETFRAMEWORK

    extension(CgCommonType @this)
    {
        public HardwareMotorTypeEnum? ToHardwareMotorTypeEnum() => @this switch
        {
            CgCommonType.Roos => HardwareMotorTypeEnum.ROOS,
            CgCommonType.OI_DOE => HardwareMotorTypeEnum.OIDOE,
            CgCommonType.NI_DOE => HardwareMotorTypeEnum.NIDOE,
            CgCommonType.OI_Relay => HardwareMotorTypeEnum.OIRelay,
            CgCommonType.NI_Relay => HardwareMotorTypeEnum.NIRelay,
            CgCommonType.OI_INC => HardwareMotorTypeEnum.OIINC,
            CgCommonType.NI_INC => HardwareMotorTypeEnum.NIINC,
            CgCommonType.OD => HardwareMotorTypeEnum.OD,

            _ => null
        };
    }

    extension(CgFFCHEnum @this)
    {
        public HardwareFourierTypeEnum? ToHardwareFourierTypeEnum() => @this switch
        {
            CgFFCHEnum.CH1 => HardwareFourierTypeEnum.Channel1,
            CgFFCHEnum.CH2 => HardwareFourierTypeEnum.Channel2,
            CgFFCHEnum.CH3_X => HardwareFourierTypeEnum.Channel3X,
            CgFFCHEnum.CH3_Y => HardwareFourierTypeEnum.Channel3Y,
            CgFFCHEnum.ALL => HardwareFourierTypeEnum.All,
            _ => null
        };
    }

    extension(CgClinderType @this)
    {
        public HardwareClinderTypeEnum? ToHardwareClinderTypeEnum() => @this switch
        {
            CgClinderType.OI_Zoos => HardwareClinderTypeEnum.OIZOOS,
            CgClinderType.NI_Zoos => HardwareClinderTypeEnum.NIZOOS,
            _ => null
        };
    }
#endif
}