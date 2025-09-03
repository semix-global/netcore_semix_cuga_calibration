using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace Core.Models.Models.Common.AODWaveform;

public abstract class AbstractAODWaveformResult<T> : ObservableObject where T : AbstractAODWaveformResult<T>
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

    protected T AdaptIn(T obj)
    {
        obj.OpticsAODElectrodeEnum = OpticsAODElectrodeEnum;
        obj.FilePath = FilePath;

        return obj;
    }
}