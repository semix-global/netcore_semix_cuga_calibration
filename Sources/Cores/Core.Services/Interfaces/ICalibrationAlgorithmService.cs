using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.StageMap;
using HalconDotNet;
using Net.Utilities.Models.Geometries;

namespace Core.Services.Interfaces;

public interface ICalibrationAlgorithmService
{
    /// <summary>
    /// 算法版本
    /// </summary>
    string Version { get; }

    #region 清晰度

    /// <summary>
    /// 获取图片清晰度, 适应彩色和灰度图像, 方差越大, 说明图像越清晰
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>清晰度</returns>
    double GetQuality(HImage image);

    /// <summary>
    /// 获取图片清晰度, 适应彩色和灰度图像, 方差越大, 说明图像越清晰
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>清晰度</returns>
    double GetDarkFieldQuality(HImage image);

    /// <summary>
    /// 获得暗场图片清晰度得分
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>清晰度</returns>
    (double XQuality, double YQuality) GetXyQuality(HImage image);

    /// <summary>
    /// 获得暗场图片调制传递函数
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="roiRect">ROI</param>
    /// <returns>MTF</returns>
    (double MtfX, double MtfY) ModulationTransferFunction(HImage image, Rect roiRect);

    /// <summary>
    /// 获得暗场图片光斑大小
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="roiRect">ROI</param>
    /// <returns>光斑大小</returns>
    (double Width, double Height) GetLightQuality(HImage image, Rect roiRect);

    #endregion 清晰度

    #region 尺寸

    /// <summary>
    /// 传入图片获取像素尺寸um
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="standardMaskSquareSize">标准掩膜方块的尺寸um</param>
    /// <param name="drawingImage">绘图图片</param>
    /// <param name="angle">网格水平夹角</param>
    /// <returns>像素尺寸um</returns>
    Size GetPixelSize(HImage image, Size standardMaskSquareSize, out HImage drawingImage, out double angle);

    /// <summary>
    /// 传入图片获取Y像素尺寸um
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="standardMaskSquareYSize">标准掩膜方块的Y尺寸um</param>
    /// <returns>Y像素尺寸um</returns>
    double GetYPixelSize(DarkFieldImageDto image, double standardMaskSquareYSize);

    #endregion 尺寸

    #region 模板匹配

    /// <summary>
    /// 传入图片创建模板并获取图片
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="image">图片</param>
    /// <param name="templateFilePath">模板路径</param>
    /// <param name="rect">尺寸</param>
    /// <param name="templateImage">模板图片</param>
    /// <returns>是否成功</returns>
    bool TryGenerateTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HImage image, string templateFilePath, Rect rect, out HImage templateImage);

    /// <summary>
    /// 读取模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="templateFilePath">模板路径</param>
    /// <param name="templateId">模板Id</param>
    /// <returns>是否成功</returns>
    bool TryReadTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, string templateFilePath, out HTuple templateId);

    /// <summary>
    /// 清除模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="templateId">模板</param>
    /// <returns>是否成功</returns>
    bool TryCleanTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HTuple templateId);

    /// <summary>
    /// 模板匹配
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="image">图片</param>
    /// <param name="templateId">模板ID</param>
    /// <param name="markPoint">位置px</param>
    /// <param name="offset">与中心偏移px</param>
    /// <param name="score">匹配得分</param>
    /// <param name="angle">匹配角度</param>
    /// <returns>是否成功</returns>
    bool TryTemplateMatchToOffset(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HImage image, HTuple templateId, out Point markPoint, out Point offset, out double score, out double angle);

    #region Projection

    /// <summary>
    /// 传入图片创建模板并获取图片
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="templateFilePath">模板路径</param>
    /// <param name="templateImage">模板图片</param>
    /// <returns>是否成功</returns>
    bool TryGenerateProjectionTemplate(HImage image, string templateFilePath, out HImage templateImage);

    /// <summary>
    /// 读取模板
    /// </summary>
    /// <param name="templateFilePath">模板路径</param>
    /// <param name="templateXId">模板XId</param>
    /// <param name="templateYId">模板YId</param>
    /// <returns>是否成功</returns>
    bool TryReadProjectionTemplate(string templateFilePath, out HTuple templateXId, out HTuple templateYId);

    /// <summary>
    /// 清除模板
    /// </summary>
    /// <param name="templateXId">模板XId</param>
    /// <param name="templateYId">模板YId</param>
    /// <returns>是否成功</returns>
    bool TryCleanProjectionTemplate(HTuple templateXId, HTuple templateYId);

    /// <summary>
    /// 模板匹配
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="templateXId">模板XId</param>
    /// <param name="templateYId">模板YId</param>
    /// <param name="point">位置px</param>
    /// <param name="offset">与中心偏移</param>
    /// <returns>是否成功</returns>
    bool TryProjectionTemplateMatchToOffset(HImage image, HTuple templateXId, HTuple templateYId, out Point point, out Point offset);

    #endregion Projection

    #endregion 模板匹配

    #region 暗场

    /// <summary>
    /// 获取raw bytes尺寸
    /// </summary>
    /// <param name="rawBytes">raw bytes</param>
    /// <returns>尺寸</returns>
    public (Size Size, long BodyBytesStartIndex, long BodyBytesLength) GetSize(byte[] rawBytes);

    /// <summary>
    /// raw body bytes add header and footer
    /// </summary>
    /// <param name="bodyBytes">raw body bytes</param>
    /// <param name="size">图片尺寸</param>
    /// <returns>raw bytes</returns>
    byte[] ToRawBytes(byte[] bodyBytes, Size size);

    /// <summary>
    /// raw bytes to 暗场图片
    /// </summary>
    /// <param name="rawBytes">raw bytes</param>
    /// <returns>暗场图片</returns>
    (HImage Image, short[,] Matrix) ToImageInfo(byte[] rawBytes);

    /// <summary>
    /// raw bytes to 暗场图片
    /// </summary>
    /// <param name="rawBytes">raw bytes</param>
    /// <returns>暗场图片</returns>
    (HImage Image, short[,] Matrix, byte[] RawBytes) ToHorizontalFlipImageInfo(byte[] rawBytes);

    /// <summary>
    /// 计算PMTGain数据
    /// </summary>
    /// <returns>是否成功</returns>
    (List<string> DatAvg, List<string> Data) GetPmtGain(Dictionary<int, List<int>> dicPmtData, int lineValue, double minValue, double maxValue);

    /// <summary>
    /// 获取D型光斑和反射光光斑 Y Angle结果集
    /// </summary>
    /// <param name="hazeImage">傅里叶相机的Haze图片</param>
    /// <param name="shinyWaferImage">傅里叶相机的ShinyWafer图片</param>
    /// <param name="rotateAngle">图像旋转角度 符号为正：逆时针 符号为负：顺时针</param>
    /// <returns>(结果绘图图像,D型光斑像素直径长度,D型光斑图像水平夹角,D型光斑中心坐标，反射光光斑中心坐标)</returns>
    (HImage drawingImage, double CenterChannelLightDiameter, double CenterChannelHorizontalDegree, Point CenterChannelLightCenterPosition, Point ReflectedLightCenterPosition) GetOpticsObjectiveYAngleResult(HImage hazeImage, HImage shinyWaferImage, double rotateAngle);

    #endregion 暗场

    #region Chuck

    /// <summary>
    /// 获取Chuck Center中心点
    /// </summary>
    /// <param name="firstTopLeftPosition">标记的左上点坐标L1</param>
    /// <param name="secondTopLeftPosition">模板匹配的左上点坐标L2</param>
    /// <param name="secondTopRightPosition">模板匹配的右上点坐标R2</param>
    /// <param name="firstTopRightPosition">标记的右上点坐标R1</param>
    /// <param name="firstBottomLeftPosition">标记的左下点坐标L4</param>
    /// <param name="secondBottomLeftPosition">模板匹配的左下点坐标L3</param>
    /// <param name="secondBottomRightPosition">模板匹配的右下点坐标R3</param>
    /// <param name="firstBottomRightPosition">标记的右下点坐标R4</param>
    /// <returns>Chuck Center中心点坐标</returns>
    Point GetChuckCenter(
        Point firstTopLeftPosition,
        Point secondTopLeftPosition,
        Point secondTopRightPosition,
        Point firstTopRightPosition,
        Point firstBottomLeftPosition,
        Point secondBottomLeftPosition,
        Point secondBottomRightPosition,
        Point firstBottomRightPosition);

    /// <summary>
    /// 计算Chuck StageMap error矩阵
    /// </summary>
    /// <param name="stageMapDto">StageMap</param>
    /// <param name="isXOnlyGantryError">X是否只包含gantry误差</param>
    /// <param name="htmlLogUniqueId">html记录日志的Id</param>
    /// <param name="calculateContainRowMinCount">算法行数包含最少行数</param>
    /// <param name="calculateContainColumnMinCount">算法列数包含最少列数</param>
    /// <param name="alignmentThreshold">对准精度</param>
    /// <param name="gantryThreshold">正交精度</param>
    /// <param name="scaleThreshold">比例精度</param>
    /// <param name="diameter">chuck直径</param>
    /// <returns>是否成功</returns>
    bool CalculateChuckStageMapError(
        StageMapDto stageMapDto,
        bool isXOnlyGantryError,
        Guid htmlLogUniqueId,
        int calculateContainRowMinCount,
        int calculateContainColumnMinCount,
        double alignmentThreshold,
        double gantryThreshold,
        double scaleThreshold,
        double diameter);

    /// <summary>
    /// 扩展基StageMapDto, 并合并StageMap(将其包含在基里面)
    /// </summary>
    /// <param name="baseStageMap">需要扩展基的StageMapDto</param>
    /// <param name="mergeStageMap">需要合并的StageMapDto</param>
    /// <param name="htmlLogUniqueId">html记录日志的Id</param>
    /// <returns>扩展后的StageMapDto</returns>
    StageMapDto ExpandStageMapDto(StageMapDto baseStageMap, StageMapDto mergeStageMap, Guid htmlLogUniqueId);

    #endregion Chuck
}