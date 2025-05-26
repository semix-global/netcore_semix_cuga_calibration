namespace Net.Utilities.Enums;

[Flags]
public enum IOCEnvironmentEnum
{
    Development = 0x0000_0001,
    Staging = 0x0000_0010,
    Production = 0x0000_0100
}