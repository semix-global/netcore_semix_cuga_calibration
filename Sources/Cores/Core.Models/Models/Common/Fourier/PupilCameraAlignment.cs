using Core.Models.Models.Common.Pattern;
using Humanizer;
using Local.SQL.DB.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models.Models.Common.Fourier;

//
// 摘要:
// FF通道号
public enum FFCH
{
    Ch1 = 1,
    Ch2,
    Ch3_X,
    Ch3_Y,
    ALL
}

public sealed partial class C2MFFRangeModel
{
    //
    // 摘要:
    //     通道12杆子最大值
    public int CH12MaxPOS;

    //
    // 摘要:
    //     通道12杆子最小值
    public int CH12MinPOS;

    //
    // 摘要:
    //     通道3转盘最大值
    public int CH3_X_LPOSMax;

    //
    // 摘要:
    //     通道3转盘最小值
    public int CH3_X_LPOSMin;

    //
    // 摘要:
    //     通道3额外挡杆位置最大值（只有X有）
    public int CH3_X_PPOSMax;

    //
    // 摘要:
    //     额外挡杆位置最小值（只有X有）
    public int CH3_X_PPOSMin;

    //
    // 摘要:
    //     通道3转盘最大值
    public int CH3_Y_LPOSMax;

    //
    // 摘要:
    //     通道3转盘最小值
    public int CH3_Y_LPOSMin;

    //
    // 摘要:
    //     通道12杆子数量
    public int RodNum;
}

