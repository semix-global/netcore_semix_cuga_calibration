using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

public class BaseService<T> where T : ISxWcfService
{
    /// <summary>
    /// 同一个接口服务, 只创建一次
    /// </summary>
    protected static T? Service { get; private set; }

    /// <summary>
    /// 是否连接成功
    /// </summary>
    protected bool IsConnected { get; set; }

    /// <summary>
    /// 创建服务
    /// </summary>
    /// <param name="sxWcfEndPoint">端口</param>
    /// <returns>是否成功</returns>
    protected SxExecuteRet<bool> CreateService(SxWcfEndPoint sxWcfEndPoint)
    {
        return Invoke(() =>
        {
            Service = Service is not null ? Service : SxWcfManager.FindService<T>(sxWcfEndPoint);
            Thread.Sleep(100);

            var sxExecuteRet = new SxExecuteRet<bool>
            {
                Success = Service is not null,
                Msg = $"CreateService: {(Service is not null ? "Success" : "Failed")}"
            };

            return sxExecuteRet;
        }, false);
    }

    /// <summary>
    /// 执行方法
    /// </summary>
    /// <typeparam name="TM">返回值类型</typeparam>
    /// <param name="callback">执行方法</param>
    /// <param name="checkServiceIsNull">是否检查服务是否为空</param>
    /// <returns>返回结果</returns>
    protected SxExecuteRet<TM> Invoke<TM>(Func<SxExecuteRet<TM>> callback, bool checkServiceIsNull = true)
    {
        var sxExecuteRet = new SxExecuteRet<TM> { Success = false };
        try
        {
            if (checkServiceIsNull && Service is null && IsConnected == false)
            {
                sxExecuteRet.Success = false;
                sxExecuteRet.Msg = "Service is null or Not Connected or Not Inited";
                return sxExecuteRet;
            }

            return callback.Invoke();
        }
        catch (Exception ex)
        {
            sxExecuteRet.Success = false;
            sxExecuteRet.Exception = ex;
            sxExecuteRet.Msg = ex.ToString();

            return sxExecuteRet;
        }
    }

    /// <summary>
    /// 执行方法
    /// </summary>
    /// <param name="callback">执行方法</param>
    /// <param name="checkServiceIsNull">是否检查服务是否为空</param>
    /// <returns>返回结果</returns>
    protected SxExecuteRet Invoke(Func<SxExecuteRet> callback, bool checkServiceIsNull = true)
    {
        var sxExecuteRet = new SxExecuteRet { Success = false };
        try
        {
            if (checkServiceIsNull && Service is null && IsConnected == false)
            {
                sxExecuteRet.Success = false;
                sxExecuteRet.Msg = "Service is null or Not Connected or Not Inited";
                return sxExecuteRet;
            }

            return callback.Invoke();
        }
        catch (Exception ex)
        {
            sxExecuteRet.Success = false;
            sxExecuteRet.Exception = ex;
            sxExecuteRet.Msg = ex.ToString();

            return sxExecuteRet;
        }
    }
}