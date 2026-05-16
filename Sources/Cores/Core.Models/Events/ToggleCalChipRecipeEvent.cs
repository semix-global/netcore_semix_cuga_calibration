using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Core.Models.Events;

public sealed class ToggleCalChipRecipeEvent
{
    public bool? IsRefreshCalChipRecipeList { get; set; } = false;

    public bool? IsEnableWaferMapEdit { get; set; } = false;

    public bool? IsCalChipRecipeAlignment { get; set; } = false;
}

public static class ToggleCalChipRecipeEventFactory
{
    public static ValueChangedMessage<ToggleCalChipRecipeEvent> RefreshCalChipRecipeManagementView(bool? value)
    {
        return new ValueChangedMessage<ToggleCalChipRecipeEvent>(new ToggleCalChipRecipeEvent
        {
            IsRefreshCalChipRecipeList = value,
            IsEnableWaferMapEdit = null,
            IsCalChipRecipeAlignment = null
        });
    }

    public static ValueChangedMessage<ToggleCalChipRecipeEvent> UpdateIsWaferMapEditEnable(bool? value)
    {
        return new ValueChangedMessage<ToggleCalChipRecipeEvent>(new ToggleCalChipRecipeEvent
        {
            IsEnableWaferMapEdit = value,
            IsRefreshCalChipRecipeList = null,
            IsCalChipRecipeAlignment = null
        });
    }

    public static ValueChangedMessage<ToggleCalChipRecipeEvent> UpdateIsCalChipRecipeAlignment(bool? value)
    {
        return new ValueChangedMessage<ToggleCalChipRecipeEvent>(new ToggleCalChipRecipeEvent
        {
            IsCalChipRecipeAlignment = value,
            IsRefreshCalChipRecipeList = null,
            IsEnableWaferMapEdit = null
        });
    }
}

