using Net.Utilities.Enums.Maths;
using Net.Utilities.Models;
using System.ComponentModel.DataAnnotations;

namespace Net.Utilities.Attributes.DataAnnotations;

/// <summary>
/// point坐标验证
/// </summary>
public sealed class PointValidationAttribute(PointEnum pointEnum) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var memberNames = validationContext.MemberName is null ? null : (string[])[validationContext.MemberName];
        var formatErrorMessage = FormatErrorMessage(validationContext.DisplayName);
        if ((!(value is Point point2d)) && (!(value is Point3D point3d))) return new ValidationResult($"{formatErrorMessage} Invalid data type. Only Point are allowed.", memberNames);
        var isOddResult = true;
        if (value is Point point)
        {
            if (point.X > 150000 * 1.1) return new ValidationResult($"{formatErrorMessage} point X {point.X} beyond the radius range.", memberNames);
            if (point.Y > 150000 * 1.1) return new ValidationResult($"{formatErrorMessage} point Y {point.Y} beyond the radius range.", memberNames);
            isOddResult = true;
        }

        if (value is Point3D) isOddResult = false;
        return pointEnum switch
        {
            PointEnum.Point2d when isOddResult => ValidationResult.Success,
            PointEnum.Point2d when isOddResult == false => new ValidationResult($"{formatErrorMessage} The value must be an point.", memberNames),
            PointEnum.Point3d when isOddResult == false => ValidationResult.Success,
            PointEnum.Point3d when isOddResult => new ValidationResult($"{formatErrorMessage} The value must be an even point3D.", memberNames),
            _ => new ValidationResult($"{formatErrorMessage} Invalid data type. Only integers are allowed.", memberNames)
        };
    }
}