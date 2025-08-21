using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace Core.Models.Models.Common.AODWaveform;

public partial class AODWaveformProfile : ObservableObject
{
    private IReadOnlyList<short> _shortList = [];
    private IReadOnlyList<byte> _byteList = [];

    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalSampleCount))]
    private int _zeroSampleCount;

    [ObservableProperty]
    private double _offsetFrequency;

    [ObservableProperty]
    private double _offsetFrequencyPeriodMultiple;

    public int TotalSampleCount => ShortList.Count + ZeroSampleCount;

    public IReadOnlyList<short> ShortList
    {
        get => _shortList;
        protected set
        {
            if (SetProperty(ref _shortList, value)) OnPropertyChanged(nameof(TotalSampleCount));
        }
    }

    public IReadOnlyList<byte> ByteList
    {
        get => _byteList;
        protected set
        {
            if (SetProperty(ref _byteList, value)) OnPropertyChanged(nameof(TotalSampleCount));
        }
    }

    partial void OnFilePathChanged(string value)
    {
        var strings = value.Split('$');
        if (strings.Length < 7) ThrowHelper.ThrowNotSupportedException("filePath name error.");

        ZeroSampleCount = short.Parse(strings[2]);
        OffsetFrequency = double.Parse(strings[5]);
        OffsetFrequencyPeriodMultiple = double.Parse(strings[6]);

        var resultString = File.ReadAllLines(value)
            .Select(t => t.Trim())
            .Where(t => string.IsNullOrWhiteSpace(t) == false)
            .ToList();

        if (resultString.Count <= 0 && resultString.All(t => t.Length == 4) == false) ThrowHelper.ThrowNotSupportedException("filePath value error.");

        ShortList = [.. resultString.Select(str => Convert.ToInt16(str, 16))];

        SetByteList(1);
    }

    protected void SetByteList(double coefficient) => SetByteList([.. Enumerable.Repeat(coefficient, ShortList.Count)]);

    protected void SetByteList(IReadOnlyList<double> coefficientWindowList)
    {
        if (ShortList.Count != coefficientWindowList.Count) ThrowHelper.ThrowNotSupportedException("Count is not equal.");

        /*
         * double[-1,1]归一化数据需要转换为16-bit或32-bit整数格式进行传输[DSP、FPGA、DAC数模转换器硬件], 目前这个是16-bit PCM(脉冲编码调制)格式
         * 1. aodWaveSignal ∈ [-1, 1] 归一化
         * 2. 放大到 [-2^15, 2^15 - 1] 之间的16位整数(Int16的取值范围)
         *          # 防止溢出的四舍五入[-1，1] * 2^15 ∈ [-2^15, 2^15 - 1]
         *          1.0  :  +32767（避免溢出到 +32768，因为 Int16 最高是 32767）
         *          0.0  :  0
         *          -1.0 :  -32768
         * 3. Int16取值范围内, < 0: 负数在二进制补码表示下，等价于加 2^32
         *          # Int16的负数补码
         *          ((short)-900).ToString("X4")                        : FC7C
         *          ((long)-900).ToString("X4")                         : FFFFFC7C
         *          ((long)-900 + (long)Math.Pow(2, 32)).ToString("X4") : FFFFFC7C
         * 4. 获取Int16所有的补码, 只会有4位
         * 5. Excel拷贝txt显示曲线:
         *          一、公式: =(HEX2DEC(A1)-IF(HEX2DEC(A1)>=32768,65536,0))/POWER(2,16)
         *          二、不要[下拉填充点]直接推拽下拉太慢, 快速公式下拉填充: 直接双击[下拉填充点]一行直接生成
         */

        /*
         * 1. 将补码转换为 [-2^15, 2^15 - 1] Convert.ToInt16(str, 16)
         * 2. 将16位整数 * 增益Rate ∈ (0,1]
         * 3. 在转换为补码
         *          # BitConverter.GetBytes, 低位在前 高位在后, 按照内存顺序返回byte[], 但是转换为Hex字符串时, 高位在前 低位在后, 所以需要反向
         *          ((short)-900).ToString("X4")                              : FC7C
         *          BitConverter.ToString(BitConverter.GetBytes((short)-900)) : 7C-FC
         *          ((short)1).ToString("x4")                                 : 0001
         *          BitConverter.ToString(BitConverter.GetBytes((short)1))    : 01-00
         */

        var result = new List<byte>();
        foreach (var bytes in ShortList.Select((value, i) => (short)(value * coefficientWindowList[i])).Select(BitConverter.GetBytes))
        {
            result.Add(bytes[1]);
            result.Add(bytes[0]);
        }

        ByteList = result;
    }
}