using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Recipe.Models.Management;

public partial class RecipeValidationResultDTO : ObservableObject
{
    [ObservableProperty]
    private bool _isValidated;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public RecipeValidationResultDTO(bool isValidated, string errorMessage)
    {
        IsValidated = isValidated;
        ErrorMessage = errorMessage;
    }

    public static RecipeValidationResultDTO Success()
        => new RecipeValidationResultDTO(true, string.Empty);

    public static RecipeValidationResultDTO Fail(string message)
        => new RecipeValidationResultDTO(false, message);

}