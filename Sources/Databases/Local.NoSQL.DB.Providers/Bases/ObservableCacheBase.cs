using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Interfaces;

namespace Local.NoSQL.DB.Providers.Bases;

public partial class ObservableCacheBase : ObservableValidator, ICacheItem
{
    [property: LiteDB.BsonId(autoId: false)]
    [ObservableProperty]
    private long _id;

    [ObservableProperty]
    private long _expiration;

    [ObservableProperty]
    private DateTime _createdTime = DateTime.Now;

    [ObservableProperty]
    private bool _isDeleted;

    public (bool IsSuccess, string ErrorMessage) Verify()
    {
        ValidateAllProperties();

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }
}