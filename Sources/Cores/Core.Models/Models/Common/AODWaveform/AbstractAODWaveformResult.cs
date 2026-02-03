using Core.Models.Enums.Optics;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.AODWaveform;

public abstract class AbstractAODWaveformResult : ObservableObject
{
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;
    private string _filePath = string.Empty;

    public OpticsAODElectrodeEnum OpticsAODElectrodeEnum
    {
        get => _opticsAODElectrodeEnum;
        internal set => SetProperty(ref _opticsAODElectrodeEnum, value);
    }

    public string FilePath
    {
        get => _filePath;
        internal set => SetProperty(ref _filePath, value);
    }

    protected T AdaptIn<T>(T obj) where T : AbstractAODWaveformResult
    {
        obj.OpticsAODElectrodeEnum = OpticsAODElectrodeEnum;
        obj.FilePath = FilePath;

        return obj;
    }
}