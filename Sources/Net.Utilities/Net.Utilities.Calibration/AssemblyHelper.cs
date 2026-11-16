using CommunityToolkit.Diagnostics;

namespace Net.Utilities.Calibration;

public static class AssemblyHelper
{
    public static string GetEmbeddedResource(string fileName)
    {
        var temp = AppDomain.CurrentDomain.GetAssemblies()
            .Where(t => t.IsDynamic == false)
            .SelectMany(t => t.GetManifestResourceNames()
                .Where(name => name.EndsWith($".{fileName}", StringComparison.OrdinalIgnoreCase))
                .Select(name => new { Assembly = t, ResourceName = name }))
            .SingleOrDefault();

        Guard.IsNotNull(temp, $"Resource '{fileName}' not found in any loaded assembly.");

        using var stream = temp.Assembly.GetManifestResourceStream(temp.ResourceName);
        Guard.IsNotNull(stream);

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}