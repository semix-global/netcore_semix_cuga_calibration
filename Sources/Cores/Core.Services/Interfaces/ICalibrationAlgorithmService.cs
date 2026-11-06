using Core.Models.Models.Common.StageMap;
using HalconDotNet;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Core.Services.Interfaces;

public partial interface ICalibrationAlgorithmService
{
    void InitialAlgorithmEngine();

    #region 清晰度

    /// <summary>
    /// 获取图片清晰度, 适应彩色和灰度图像, 方差越大, 说明图像越清晰
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="guid">日志Id</param>
    /// <param name="algorithmEngineTypeEnum">算法库</param>
    /// <returns>清晰度</returns>
    double GetQuality(BitmapImage image, Guid guid, AlgorithmEngineTypeEnum algorithmEngineTypeEnum);

    /// <summary>
    /// 获取图片清晰度, 适应彩色和灰度图像, 方差越大, 说明图像越清晰
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="guid">日志Id</param>
    /// <returns>清晰度</returns>
    double GetDarkFieldQuality(BitmapImage image, Guid guid);

    /// <summary>
    /// 获得暗场图片清晰度得分
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="guid">日志Id</param>
    /// <returns>清晰度</returns>
    (double XQuality, double YQuality) GetXyQuality(BitmapImage image, Guid guid);

    /// <summary>
    /// 获得暗场图片调制传递函数
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="roiRect">ROI</param>
    /// <param name="guid">日志Id</param>
    /// <returns>MTF</returns>
    (double MtfX, double MtfY) ModulationTransferFunction(BitmapImage image, Rect roiRect, Guid guid);

    #endregion 清晰度

    #region 暗场

    /// <summary>
    /// 获取D型光斑和反射光光斑 Y Angle结果集
    /// </summary>
    /// <param name="hazeImage">傅里叶相机的Haze图片</param>
    /// <param name="shinyWaferImage">傅里叶相机的ShinyWafer图片</param>
    /// <param name="rotateAngle">图像旋转角度 符号为正：逆时针 符号为负：顺时针</param>
    /// <param name="guid">日志Id</param>
    /// <returns>(结果绘图图像,D型光斑像素直径长度,D型光斑图像水平夹角,D型光斑中心坐标，反射光光斑中心坐标)</returns>
    (BitmapImage drawingImage, double CenterChannelLightDiameter, double CenterChannelHorizontalDegree, Point CenterChannelLightCenterPosition, Point ReflectedLightCenterPosition) GetOpticsObjectiveYAngleResult(BitmapImage hazeImage, BitmapImage shinyWaferImage,
        double rotateAngle, Guid guid);

    /// <summary>
    /// 获得图像两端ROOS遮挡后的明暗分界线中心Y像素坐标
    /// </summary>
    /// <param name="image"></param>
    /// <param name="drawImage"></param>
    /// <returns>(起点pix，终点pix)</returns>
    (double StartImageYPixel, double EndImageYPixel) GetOpticsROOSResult(BitmapImage image, out HImage drawImage);

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
    Point GetChuckCenter(Point firstTopLeftPosition,
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
    /// <param name="guid"></param>
    /// <param name="calculateContainRowMinCount">算法行数包含最少行数</param>
    /// <param name="calculateContainColumnMinCount">算法列数包含最少列数</param>
    /// <param name="alignmentThreshold">对准精度</param>
    /// <param name="gantryThreshold">正交精度</param>
    /// <param name="scaleThreshold">比例精度</param>
    /// <param name="diameter">chuck直径</param>
    /// <returns>是否成功</returns>
    bool CalculateChuckStageMapError(StageMapDto stageMapDto,
        bool isXOnlyGantryError,
        Guid guid,
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
    /// <param name="htmlLogUniqueId"></param>
    /// <returns>扩展后的StageMapDto</returns>
    StageMapDto ExpandStageMapDto(StageMapDto baseStageMap, StageMapDto mergeStageMap, Guid htmlLogUniqueId);

    #endregion Chuck

    #region 工具

    double[] GetImageGrayYProjectionsPixels(BitmapImage image, Guid guid);

    (Point CenterPosition, double Radius) FitCircle(IReadOnlyList<Point> points);

    #endregion
}