## 2.0.15.1111


>   1.   权限管理增加, 默认用户名admin, 密码666666
>   2.   增加：散光校准、pmt Gain校准、chuck auto focus
>   3.   优化：均匀性校准3.0，aod delay2.0
>   4.   新的日志架构增加
>   5.   ADS 日志、界面、切换速度优化

## 2.0.15.1113


>   1.   HTML日志层面优化：1. 解决堆栈追踪问题：AddCallSiteHiddenAssembly 2. 解决clear log proviers 3. 渲染异常日志可以通过配置获取 4. 添加转义
>   2.   ADS验证的时候bugfix修复


## 2.0.15.1114


>   1.   均匀性下发波形bug fix、均匀性校准逻辑bug fix
>   2.   nuget包更新
>   3.   .net 480 remove runtimehelper, 统一使用`AsSpan()`https://github.com/Sergio0694/PolySharp/issues/8
>   4.   ADS Y Gain 下发寄存器bugfix修复
>   5.   算法模块的标准化
>   6.   HTML日志时间戳优化(通过nlog.config配置)


## 2.0.15.1115


>   1.   前台显示bug fix

## 2.0.16.1206


  >   1.   新增pmt gain2.0、散光校准2.0、均匀性校准3.1、XTC2.0、激光指向校准优化
  >   2.   添加行扫、鼠标领航界面、RTFC
  >   3.   新增权限分配管理界面

## 2.0.16.1226


  >   1.   暗场明场的位置校准，添加倍率设置，方便cuga计算换算关系
  >   2.   Move Stage页面添加Wafermap图显示
  >   3.   暗场对准
  >   4.   stage日志以及误差分析
  >   5.   全局比例误差校准
  >   6.   ads、microscope、chuck日志升级,以及日志优化卡顿问题
  >   7.   均匀性原始数据采集工具
  >   8.   复机校准流程工具
  >   9.   Attenuator、OpticalPowerMeter、AutoFocus优化

## 2.1.1.0122


  >   1.   新增：明场stagemap校准2.0 采图改为外接矩形，可以采图全部(对应日志体系修改外接圆）、Prescan和Chirp AOD同频率校准1.0、 ADS质量重心校准、ADS X校准自动化流程2.0、ADS Y校准自动化流程2.0
  >   2.   新增：ADS诊断采集分析工具
  >   3.   新增：Laser PixelSize、 Line Centricity 校准增加屏蔽部分PMT功能、验证增加多选功能、均匀性校准一一对应关系4.0、数据数据采集分析
  >   4.   新增：校准L1认证DSW规范化、校准依赖关系规范化
  >   5.   优化：暗场采图接口prescan波形chirp波形以及功率等由校准自定义（整体暗场采图架构流程修改优化）、（chuck以及机械扫图）机械采图坐标系按照cuga传参修正方向、行扫根据算法byte[]补齐、Move Stage页面添加map图显示，以及根据明暗场切换显示（以及可以PTP）
  >   6.   新增：缓存架构嵌入到整体项目（ADS 、显微镜、chuck、暗场激光的所有校准中）、以及DTO映射mapster映射规则的优化和修改
  >   7.   优化HTML：Laser Aod Delay、Line Centricity、Pixel Size、XY Astigmatism、Attenuator，AutoFocus，BeamStabilizer，PmtGain，XTC、Common流程拆分、日志改造
  >   8.   新增HTML：日志体系中，图片日志模块日志显示存在overlay，(用于各种模板匹配日志)


## 2.1.1.0318


  >   1.   新增：
  >        1.   暗场stagemap校准3.0 采图改为外接矩形，可以采图全部(对应日志体系修改外接圆）
  >        2.   Linecentricity2.0添加扫描正反向误差、均匀性校准Prescan对应关系5.0，多V实现对准，CH1,CH2通道代码功能实现
  >        3.   XTC2.0校准开发
  >        4.   gantry2.0校准开发
  >        5.   校准依赖关系规范、校准顺序分级
  >        6.   全尺寸stagemap合并1.0
  >        7.   自动化校准配方模块
  >        8.   AOD多项式补偿 Prescan Chirp生成
  >   2.   优化：HTML TAB模块开发，多个日志图片验证TAB模块、 html日志 菜单栏模块根据日志颜色显示颜色、HTML下载模块（2d数据，3d数据都独立出来）


## 2.1.1.0328


  >   1.   bugfix: 修复prealigner保存litedb文件大异常、litedb获取数据异常设置缓存为空异常
  >   2.   新增：aod 生成支持负数

## 2.1.2.0416


  >   1.   明场新增：focus、centricity、pixelsize、gantry自动化校准
  >   2.   暗场新增：xpixelsize校准、优化散光校准
  >   3.   ADS2.0校准：X(低中高mag * 低中高速度 = 9), Y(100速度)
  >   4.   诊断功能：新增chirp prescan 功率均匀性，chirp波形训练
  >   5.   校准文档发布：新的校准结果配合cuga
  >   6.   配方管理
  >   7.   配合cuga升级对准接口版本更新
  >   8.   修复：prealigner验证

## 2.1.0.0424

>   1.   优化HTML日志：日志过大不保存问题解决
>   2.   新的校准结果文件下发
>   3.   新的校准结果文档
>   4.   自动化校准：chuck global scale
>   5.   明场新增：chuck rotate scale


## 2.1.0.0516
>   1. 自动化校准：Chuck 旋转比例、Laser Y Pixel Size、Laser Line centricity、ads pressure、ads x gain
>	2. ADS: ads y gain 3.0
>   3. 新的自动化界面设计、自动化review

## 2.1.0.0523

>   1.   诊断：焦点飘逸RTFC
>   2.   自动化校准：ADS Y gain、Laser Pixel Size X、Chuck Prealigner
>   3.   设置：初始化报警自检

## 2.1.0.0705

>   1.   诊断：ADS 手动
>   2.   自动化校准：stagemap
>   3.   Laser校准：auto focus 2.0、agc delay1.0
>   4.   设置：配方数据库分离 标准库替换 

## 2.1.0.0822

>   1.   优化：
>
>        11# AODtraing添加lightpoint strehl ratio  、 StrehlRatio
>
>        12# AGC 优化(修改并发方式) 2.0.10版本net utilities更新 
>
>        22# 长扫图3G 以上 内存开销小速度快分割匹配的方法 
>
>        25# 双电极AOD算法 
>
>        28# 自动化校准采图模式：agc 反差， agc开关，Gain 设置 LOK等、 添加calchip不判断是否有晶圆 、 修改agc等自动化接口
>
>        29# 多电极版本、散光临时修改算法为mtf
>
>        30# wafermap转机械坐标、增加review
>
>        35# 校准采图接口长扫图不报错fix，截取模板光强固定fix
>
>        37# 多电极波形生成、 多电极结果集的保存和互转
>
>        Feature/dev stagemap bugfix
>
>        采图方法应用setting af参数
>
>   2.   校准：
>
>        21# calchip2.0/采图模式配置/重命名/倍镜反序列化映射写入
>
>        23# 动态配置倍镜、 B3镜头动态配置的bug修复
>
>        26# 均匀性校准改为单光斑
>
>        27# CalChip2.0
>
>        31# microscope、chuck傻瓜化，倍镜动态配置bug修复，calchip rtfc