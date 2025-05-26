using Net.Utilities.Enums.Maths;
using System.ComponentModel.DataAnnotations;

namespace Net.Utilities.Attributes.DataAnnotations;

/// <summary>
/// 奇数偶数验证
/// </summary>
public sealed class OddEvenNumberAttribute(ParityEnum parityEnum) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var memberNames = validationContext.MemberName is null ? null : (string[])[validationContext.MemberName];
        var formatErrorMessage = FormatErrorMessage(validationContext.DisplayName);

        if (int.TryParse(value?.ToString(), out var result) == false) return new ValidationResult($"{formatErrorMessage} Invalid data type. Only integers are allowed.", memberNames);

        var isOddResult = (result & 1) == 1;

        return parityEnum switch
        {
            ParityEnum.Odd when isOddResult => ValidationResult.Success,
            ParityEnum.Odd when isOddResult == false => new ValidationResult($"{formatErrorMessage} The value must be an odd number.", memberNames),
            ParityEnum.Even when isOddResult == false => ValidationResult.Success,
            ParityEnum.Even when isOddResult => new ValidationResult($"{formatErrorMessage} The value must be an even number.", memberNames),
            _ => new ValidationResult($"{formatErrorMessage} Invalid data type. Only integers are allowed.", memberNames)
        };
    }
}