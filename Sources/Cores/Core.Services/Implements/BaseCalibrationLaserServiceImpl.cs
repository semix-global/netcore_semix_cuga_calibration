using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Semix.CoreLib;
using System.IO;

#if NET
// ReSharper disable once CheckNamespace
namespace Core.Services.Implements.GRPC;
#else
// ReSharper disable once CheckNamespace
namespace Core.Services.Implements.WCF;
#endif

public sealed partial class CalibrationLaserServiceImpl
{
    public SxExecuteRet<DarkFieldPrescanDto> ReadPrescanByFile(string filePath, double coefficient)
    {
        var prescanDto = new DarkFieldPrescanDto();

        var strings = filePath.Split('$');
        if (strings.Length < 3) throw new ArgumentException("filePath name error.");
        prescanDto.RegNum = short.Parse(strings[1]);
        prescanDto.ZeroNum = short.Parse(strings[2]);
        var resultString = File.ReadAllLines(filePath).Select(t => t.Trim()).Where(t => string.IsNullOrWhiteSpace(t) == false).ToList();
        if (resultString.Count <= 0 && resultString.All(t => t.Length == 4) == false) throw new ArgumentException("filePath value error.");

        prescanDto.PrescanList = [.. resultString.Select(str => Convert.ToInt16(str, 16))];

        return SetPrescanByRate(prescanDto, [.. Enumerable.Repeat(coefficient, prescanDto.PrescanList.Count)]);
    }

    public SxExecuteRet<DarkFieldPrescanDto> SetPrescanByRate(DarkFieldPrescanDto darkFieldPrescanDto, List<double> prescanRateList)
    {
        var dto = darkFieldPrescanDto.Clone();

        if (dto.PrescanList.Count != prescanRateList.Count) throw new ArgumentException("PrescanList and PrescanRateList count is not equal.");

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
        foreach (var compArray in dto.PrescanList.Select((value, i) => (short)(value * prescanRateList[i])).Select(BitConverter.GetBytes))
        {
            result.Add(compArray[1]);
            result.Add(compArray[0]);
        }

        dto.PrescanByteList = result;

        return SxExecuteRetHelper.CreateSuccess(dto);
    }

    public SxExecuteRet<DarkFieldChirpAodWaveDto> ReadChirpAodByCustomFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var chirpAodWaveDto = ResolveAodFileNameToDarkFieldChirpAodWaveDto(fileName).Clone();
        chirpAodWaveDto.IncrementChirpAodFilePath = filePath;

        var resultString = File.ReadAllLines(filePath).Select(t => t.Trim()).Where(t => string.IsNullOrWhiteSpace(t) == false).ToList();
        if (resultString.Count <= 0 && resultString.All(t => t.Length == 4) == false) throw new ArgumentException("filePath value error.");

        chirpAodWaveDto.ChirpAodWaveList = [.. resultString.Select(str => Convert.ToInt16(str, 16))];

        var result = new List<byte>();
        foreach (var compArray in chirpAodWaveDto.ChirpAodWaveList.Select(BitConverter.GetBytes))
        {
            result.Add(compArray[1]);
            result.Add(compArray[0]);
        }

        chirpAodWaveDto.ChirpAodWaveByteList = result;
        //chirpAodWaveDto.OriRegNum=(short)chirpAodWaveDto.ChirpAodWaveList.Count;
        return SxExecuteRetHelper.CreateSuccess(chirpAodWaveDto);
    }

    public SxExecuteRet<DarkFieldChirpAodWaveDto> ReadChirpAodByConfigFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var strAry = fileName.Split('$');
        if (strAry is null || strAry.Length != 6) throw new ArgumentException("Chirp Aod Config File Path is Error.");
        var chirpAodWaveDto = new DarkFieldChirpAodWaveDto
        {
            IncrementChirpAodFilePath = filePath,
            ZeroNum = short.Parse(strAry[2])
        };
        var resultString = File.ReadAllLines(filePath).Select(t => t.Trim()).Where(t => string.IsNullOrWhiteSpace(t) == false).ToList();
        if (resultString.Count <= 0 && resultString.All(t => t.Length == 4) == false) throw new ArgumentException("filePath value error.");

        chirpAodWaveDto.ChirpAodWaveList = [.. resultString.Select(str => Convert.ToInt16(str, 16))];
        chirpAodWaveDto.OriRegNum = Convert.ToInt16(resultString.Count);

        var result = new List<byte>();
        foreach (var compArray in chirpAodWaveDto.ChirpAodWaveList.Select(BitConverter.GetBytes))
        {
            result.Add(compArray[1]);
            result.Add(compArray[0]);
        }

        chirpAodWaveDto.ChirpAodWaveByteList = result;
        return SxExecuteRetHelper.CreateSuccess(chirpAodWaveDto);
    }

    public SxExecuteRet<DarkFieldChirpAodWaveDto> GetChirpAodByChangeRateFromFile(DarkFieldChirpAodWaveDto currentDarkFieldChirpAodWaveDto, double rateChange)
    {
        var chirpAodWaveDto = new DarkFieldChirpAodWaveDto
        {
            SoundPackageLength = currentDarkFieldChirpAodWaveDto.SoundPackageLength,
            CenterFrequency = currentDarkFieldChirpAodWaveDto.CenterFrequency,
            ZeroNum = currentDarkFieldChirpAodWaveDto.ZeroNum,
            RateChange = rateChange,
            IncrementChirpAodFilePath = currentDarkFieldChirpAodWaveDto.IncrementChirpAodFilePath,
        };

        var files = Directory.GetFiles(Path.GetDirectoryName(currentDarkFieldChirpAodWaveDto.IncrementChirpAodFilePath)!, "*.txt", SearchOption.AllDirectories);

        var aodFilePath = files
            .Select(t => (FilePath: t, Result: ResolveAodFileNameToDarkFieldChirpAodWaveDto(Path.GetFileName(t))))
            .Where(t => Math.Abs(t.Result.RateChange - rateChange) <= 0.00001 //  0.00001为防止double精度丢失的问题对比不一致
                        && t.Result.SoundPackageLength - currentDarkFieldChirpAodWaveDto.SoundPackageLength == 0
                        && t.Result.CenterFrequency - chirpAodWaveDto.CenterFrequency <= 0.01) // 0.01为防止double精度丢失的问题对比不一致
            /*&& ((t.Result.BandWidthHigh- t.Result.BandWidthLow)/2+ t.Result.BandWidthLow- chirpAodWaveDto.CenterFrequency)<=0.01)*/
            .Select(t => t.FilePath)
            .FirstOrDefault();

        return aodFilePath is not null
            ? ReadChirpAodByCustomFile(aodFilePath)
            : SxExecuteRetHelper.CreateError("Chirp Aod File Is Not Exists Current Input Params", chirpAodWaveDto);
    }

    private static DarkFieldChirpAodWaveDto ResolveAodFileNameToDarkFieldChirpAodWaveDto(string fileName)
    {
        var chirpAodWaveDto = new DarkFieldChirpAodWaveDto();
        var strAry = fileName.Split('_');
        if (strAry is null || strAry.Length < 11) throw new ArgumentException("Chirp Aod File Path is Error.");
        var soundPackageLength = double.Parse(strAry[1].Replace("mm", string.Empty));
        var bandWidthHigh = double.Parse(strAry[3].Replace("Mhz", string.Empty));
        var bandWidthLow = double.Parse(strAry[4].Replace("Mhz", string.Empty));
        chirpAodWaveDto.OriRegNum = short.Parse(strAry[10].Split('$')[1]);
        chirpAodWaveDto.BandWidthHigh = bandWidthHigh;
        chirpAodWaveDto.BandWidthLow = bandWidthLow;
        chirpAodWaveDto.SoundPackageLength = soundPackageLength;
        chirpAodWaveDto.BandWidth = bandWidthHigh - bandWidthLow;
        chirpAodWaveDto.CenterFrequency = bandWidthLow + chirpAodWaveDto.BandWidth / 2;
        return chirpAodWaveDto;
    }
}