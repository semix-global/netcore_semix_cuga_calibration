using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Core.Models.Events;

public sealed class ToggleRecipeEvent
{
    public bool? IsRefreshRecipeList { get; set; } = false;

    public bool? IsEnableWaferMapEdit { get; set; } = false;

}

public static class ToggleRecipeEventFactory
{
    public static ValueChangedMessage<ToggleRecipeEvent> RefreshRecipeManagementView(bool? value)
    {
        return new ValueChangedMessage<ToggleRecipeEvent>(new ToggleRecipeEvent
        {
            IsRefreshRecipeList = value
        });
    }

    public static ValueChangedMessage<ToggleRecipeEvent> UpdateIsWaferMapEditEnable(bool value)
    {
        return new ValueChangedMessage<ToggleRecipeEvent>(new ToggleRecipeEvent
        {
            IsEnableWaferMapEdit = value
        });
    }
}
