using Core.Models.Helper;
using Semix.CoreLib;
using Semix.CoreLib.Grpc.Client;

namespace Core.Services.Implements.GRPC;

public class BaseService<T> where T : class
{
    private const string ErrorMessage = "Service is null or Not Connected or Not Init";

    protected T? Service { get; private set; }

    protected bool IsConnected { get; set; }

    protected virtual SxExecuteRet<bool> CreateService()
    {
        if (Service is not null) return SxExecuteRetHelper.CreateSuccess(true);

        var findService = Client.Instance.FindService<T>();
        if (findService.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(findService.ErrorMsg);

        Service = findService.Anything;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    protected SxExecuteRet<TM> Invoke<TM>(Func<SxExecuteRet<TM>?> callback)
    {
        var sxExecuteRet = new SxExecuteRet<TM> { Success = false };
        try
        {
            return callback.Invoke() ?? new SxExecuteRet<TM> { Success = false, ErrorMsg = ErrorMessage };
        }
        catch (Exception ex)
        {
            sxExecuteRet.Success = false;
            sxExecuteRet.Exception = ex;
            sxExecuteRet.Msg = ex.ToString();

            return sxExecuteRet;
        }
    }

    protected SxExecuteRet Invoke(Func<SxExecuteRet?> callback)
    {
        var sxExecuteRet = new SxExecuteRet { Success = false };
        try
        {
            return callback.Invoke() ?? new SxExecuteRet { Success = false, ErrorMsg = ErrorMessage };
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

public class BaseService<T, TM> : BaseService<T> where T : class where TM : class
{
    protected TM? Service2 { get; private set; }

    protected override SxExecuteRet<bool> CreateService()
    {
        var sxExecuteRet = base.CreateService();
        if (sxExecuteRet.IsSuccess == false) return sxExecuteRet;

        if (Service2 is not null) return SxExecuteRetHelper.CreateSuccess(true);

        var findService = Client.Instance.FindService<TM>();
        if (findService.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(findService.ErrorMsg);

        Service2 = findService.Anything;
        return SxExecuteRetHelper.CreateSuccess(true);
    }
}

public class BaseService<T, TM, TN> : BaseService<T, TM> where T : class where TM : class where TN : class
{
    protected TN? Service3 { get; private set; }

    protected override SxExecuteRet<bool> CreateService()
    {
        var sxExecuteRet = base.CreateService();
        if (sxExecuteRet.IsSuccess == false) return sxExecuteRet;

        if (Service3 is not null) return SxExecuteRetHelper.CreateSuccess(true);

        var findService = Client.Instance.FindService<TN>();
        if (findService.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(findService.ErrorMsg);

        Service3 = findService.Anything;
        return SxExecuteRetHelper.CreateSuccess(true);
    }
}

public class BaseService<T, TM, TN, TO> : BaseService<T, TM, TN> where T : class where TM : class where TN : class where TO : class
{
    protected TO? Service4 { get; private set; }

    protected override SxExecuteRet<bool> CreateService()
    {
        var sxExecuteRet = base.CreateService();
        if (sxExecuteRet.IsSuccess == false) return sxExecuteRet;

        if (Service4 is not null) return SxExecuteRetHelper.CreateSuccess(true);

        var findService = Client.Instance.FindService<TO>();
        if (findService.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(findService.ErrorMsg);

        Service4 = findService.Anything;
        return SxExecuteRetHelper.CreateSuccess(true);
    }
}

file static class Client
{
    private static readonly Lazy<SxGrpcClient> InstanceConstructor = new(() =>
    {
        var sxGrpcClient = new SxGrpcClient();
        sxGrpcClient.Connect(port: 3000);

        return sxGrpcClient;
    }, LazyThreadSafetyMode.PublicationOnly);

    internal static SxGrpcClient Instance => InstanceConstructor.Value;
}