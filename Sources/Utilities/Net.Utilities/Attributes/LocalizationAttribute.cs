namespace Net.Utilities.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class LocalizationAttribute : Attribute
{
    public required string ResourceName { get; init; }
    public required Type ResourceType { get; init; }
}