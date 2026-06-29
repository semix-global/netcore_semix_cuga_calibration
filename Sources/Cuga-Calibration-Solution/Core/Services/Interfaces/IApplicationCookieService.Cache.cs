namespace CugaCalibration.Core.Services.Interfaces;

public partial interface IApplicationCookieService
{
    object GetOrDefault(Type type, bool isRecipe, CancellationToken cancellationToken = default);

    object[] GetArrayOrDefault(Type type, bool isRecipe, CancellationToken cancellationToken = default);

    void Set(Type type, object cache, bool isRecipe, CancellationToken cancellationToken = default);

    void SetArray(Type type, object[] caches, bool isRecipe, CancellationToken cancellationToken = default);
}