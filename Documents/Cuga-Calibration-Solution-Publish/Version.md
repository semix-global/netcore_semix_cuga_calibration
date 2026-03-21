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
>        35# 校准采图接口长扫图不报错fix，截取模板光强固定fix、35# 移除preview语法特性 bug fix version
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

## 2.2.0.1015

>   1.   优化：
>
>        38# fix error: laser auto focus、raw image get size over flow、x pixel
>
>        39# 禁用校准状态bug修复
>
>        40# LaserLightInformation全局替换掉Coefficient, service架构重构、40# GetNmPerEcs GetEcsPerOffsetMotorMm GetDeviceCode, 日志优化#40 多电极tools、laser light全部替换、
>
>        #41 去除constantHelper校准小项注解，wcf和dto互转用反射实现
>
>        #44 modify stage peg、peg X使用stagemap进行软补偿，不使用acs 
>
>        #48 x pixel size error bug fix、 #48 Linecentricity改为机械坐标，优化体验、 #48 prealigner 界面傻瓜化bug fix、 #48 global scale 傻瓜化 bug fix、 #48 microscope centricity bug fix、#48 agc 下发 bug fix
>
>        #51 CUGA升级 adsspeed移除
>
>   2.   校准：
>
>        35# dark auto focus gain 3.0
>        37# IP双电极版本、37# 显微镜models架构重构、Laser light info by cuga
>
>        36# DOE rotate angle1.0
>
>        33# 多电极散光校准、33# LineCentricity 校准流程取消下发、33# base公开ApplicationCookies属性，修复界面倍镜列表显示为空的bug、33# 修复Y Pixel Size 界面显示错乱变成DOE角度校准界面的问题、33# 增加多光斑RTFC数据分析的matlab脚本
>
>        45# 自动发布安装exe、软件关于
>
>        #46 XTC双电极
>
>        #47 AOD相位延迟校准工具
>
>        feat: prealigner角度补偿改成绝对值

## 2.3.0.1120

> 1.   优化：
>
>      #50 校准中涉及到RTFC的步骤（CalChip、DOE校准），其结果图片会打印日志，方便查看。公共参数增加默认低倍高倍倍镜配置，校准中涉及到配置倍镜的部分初始化时从公共参数同步。
>
>      #53 chuck center 验证方式更改为下发后验证，line centricity验证方式更改为和cuga same points相同的逻辑
>
>      #54 移除ApplicationCookies中倍镜缓存列表，校准IsOK方法中取消判断cuga配置倍镜和校准缓存倍镜不一致的逻辑，简洁代码
>
>      #58 暗场stagemap包含gantry误差，用于缩小缺陷定位误差
>
>      #59 scottplot图表的架构以及绑定架构
>
>      #60 dark stage map x only gantry、#60 MAG Speed改为S40 S50产率架构
>
>      #61 optical power meter, attenuator AOD delay 傻瓜式校准优化(并且修改为产率架构)、#61 remove morelinq
>
>      #65 Line Centricity/ XY Pixel Size/ XTC /XY Astigmatism /Stage Map mag、speed更改为产率
>
>      #74 Cuga自检配置优化，集合类型校准结果为空时，序列化时插入一个默认的对象元素
>
>      #76 prealigner区别示教和验证的阈值，增加防呆，修复验证时offset阈值过小导致的reload wafer无法使用校准值补偿，界面重构简化，优化用户体验
>
> 2.   校准：
>
>      #24 ADS4.0 优化完善ADS校准，最大限度提升性能，解决之前的软件BUG
>
>      #42 AutoFocus4.0 暗场自动聚焦新功能开发(AF与ECS标定)
>
>      #49 ADS诊断工具1.0 按照制定位置起点终点，速度然后动态获取校准结果后输出HTML日志
>
>      #52 AF诊断工具1.0 按照制定位置模式等获取所有nsc曲线, AF添加一个自动化找offset对应的ecs方法
>
>      #56 照明和采集对齐校准工具1.0
>
>      #57 暗场正反向offset校准1.0
>
>      #71 多电极相位延迟校准工具初版本1.0
>
>      #75 XPixelSize3.0 滑动窗口匹配
>
>      #77 objective Y Angle校准工具1.0
>      
>      #81 MMD1.0
>      
>      #84 下发波形文件配置

## 2.5.0.0309

>   1. 新增校准、诊断
>
>       #81 MMD 1.1
>
>       #86 MMD1.2
>
>       #88 明暗场对准offset校准
>
>       #90 4电极初始化程序
>
>       #91 Prescan AOD 相位校准1.0
>
>       #93 OI NI Relay1.0
>
>       #94 BestFocus
>
>       #101 Light Matching
>
>       #105 Optics INC
>
>       #108 AOD Uniformity、算法图像新结构
>
>       #110 采集偏振Analyzer校准1.0 版 Loading依赖项各校准获取方式更正
>
>       #114 Global Focus Offset
>
>       #116 AOD Waveform And Training
>
>       #120 MMD1.3、CollectionFocusAlignOpticsFocus、OpticsObjectiveYAngle这两个校准同步KLA
>
>       #127 DF Calchip
>
>       \#134 AUTO FOCUS修改低中高2.0
>
>       \#138 OI NI Relay2.0 Optics Relay添加X/Z同步运动
>
>   2.   架构
>
>        #92 X/Y Pixel Size、Line Centricity 选择OINI、选择产率 界面改成ListBox
>
>        #115 权限的源生成器
>
>        #120 cache 导入导出弹窗以及不需要的属性删除
>
>        #121 缓存导入导出
>
>        #126 移除litedb
>
>        #129  所有已发布的校准启动页面Loading相互依赖关系2.0,load依赖项代码规范化
>
>        #130 校准的架构调整：OpticsMagTypeEnum 彻底移除，包含相关服务分离（laser分离到具体比如optics，cib等）
>
>        \#140 CUGA 登录用户权限控制
>
>        \#141 采图诊断工具2.0, setting页面2.0
>
>   3.   优化
>
>        #24  新增加了接口，将Cuga自动速度移动chuck修改为固定速度
>
>        #63 Y Pixel Size Line Centricity 增加垂直入射 
>        #68 X Pixel Size 校准3.0（NI路）
>
>        #69 激光DOE Angle校准修改为产率版本，并合并到主分支
>
>        #70 line orientation正反向校准修改为产率模式，增加了操作步骤
>
>        #78 chuck center和theta scale合二为一，global和theta scale改成累乘，简化选点步骤#87 calchip重构，暗场验证方式改成清晰度的方式
>
>        #91 prescan chirp 多电极校准工具统一 
>
>        #100 OI NI AOD Alignment Delay Laser Optics Power Meter Laser Attenutar 
>        #104 CIB Illumination Profile 
>
>        #106 Prealigner校准流程改为循环n次
>        #107 calchip dsw Alignment
>
>        #108 CIB XTC
>
>        #109 X pixel size采图线数优化新增需求
>
>        #113 校准兼容chuck和dsw模式
>
>        #116 AOD Waveform And Training 优化
>        #117 Global Field Tilt方案修改，兼容遍历和bestfocus
>
>        #124 y pixel size迁移到CIB目录，替换算法
>
>        #125 CIB Line Centricity迁移到CIB目录，替换算法
>
>        #126 y Strehl 比添加二次项
>
>        #132 转线型图bugfix
>
>        \#142 线性图采图接口转换, x pixel size算法MAD NMS 优化

## 2.5.0320

>   1.   校准
>
>        #143 LM Silica Spheres 目前改为都是haze方案， calchip校准 明场pixelsize没有的情况下用默认
>
>   2.   优化
>
>        #143 X Pixel Size算法优化，V Sharp 算法优化，线型图修改不缩放，单元测试算法matlab架构