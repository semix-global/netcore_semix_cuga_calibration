using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;

namespace Core.Models.Models.Common.Recipe;

public partial class CalibrationRecipeDtoBase : ObservableObject, IEntityAdd
{
    [ObservableProperty]
    private long _createdUserId;

    [ObservableProperty]
    private string _createdUserName = string.Empty;
}