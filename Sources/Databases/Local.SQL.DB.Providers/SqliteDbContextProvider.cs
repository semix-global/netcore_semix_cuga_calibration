using FreeSql;
using FreeSql.Aop;
using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Attributes;
using Local.SQL.DB.Providers.Models.Entities;
using Local.SQL.DB.Providers.Models.Entities.Base;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Local.SQL.DB.Providers.Models.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniExcelLibs;
using Net.Utilities.Helper.File;
using Net.Utilities.Helper.Object.String;
using Net.Utilities.Models;
using SourceGenerator.InjectHostDI;
using System.Reflection;
using System.Text.RegularExpressions;
using Yitter.IdGenerator;
using SysUserDto = Local.SQL.DB.Providers.Models.Entities.DTO.SysUserDto;

#if NETFRAMEWORK
using MoreLinq;

#endif

namespace Local.SQL.DB.Providers;

public static class SqliteDbContextProvider
{
    private static readonly Regex Regex = new($"Data Source=(?<{nameof(Directory)}>[^;]+)");

    public static IServiceCollection AddSqlDbContext(this IServiceCollection services, IHostEnvironment hostEnvironment)
    {
        services.AddSingleton(_ => new SysUserDto());

        // 看了源码可以WPF使用单例模式(全局一个事务), Scope是因为每次HTTP请求事务独立
        services.AddSingleton(GetFreeSql);
        services.AddSingleton(sp => new UnitOfWorkManager(sp.GetRequiredService<IFreeSql>()));

        services.AddLocalSQLDBProvidersInjectHostDI(hostEnvironment);

        return services;
    }

    #region Freesql

    private static IFreeSql GetFreeSql(IServiceProvider serviceProvider)
    {
        if (YitIdHelper.IdGenInstance is null)
            YitIdHelper.SetIdGenerator(new IdGeneratorOptions
            {
                WorkerId = 1,
                WorkerIdBitLength = 6,
                SeqBitLength = 6
            });

        var appSettingOptions = serviceProvider.GetRequiredService<IOptions<ApplicationSetting>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<LogProvider>>();
        var sysUserDto = serviceProvider.GetRequiredService<SysUserDto>();

        var match = Regex.Match(appSettingOptions.SqlDbDataSource);
        if (match.Success == false) throw new ArgumentException("SqlDbDataSource is invalid");

        var dataSourcePath = match.Groups[nameof(Directory)].Value;
        DirectoryHelper.CreateFileDirectoryIfNotExists(dataSourcePath);

        var freeSql = new FreeSqlBuilder()
            .UseConnectionString(DataType.Sqlite, appSettingOptions.SqlDbDataSource)
            .UseAutoSyncStructure(false)
            .UseLazyLoading(false)
            .UseNoneCommandParameter(true)
            .Build();

        // 监听Curd操作, _ 是 update,insert,delete,update对象
        freeSql.Aop.CurdBefore += (_, e) => logger.LogTrace("Sql: {@Sql}", e.Sql);
        freeSql.Aop.CurdAfter += (_, e) => logger.LogTrace("CurdAfter: {@ElapsedMilliseconds}", e.ElapsedMilliseconds);

        // 同步数据
        SyncStructure(freeSql);
        SyncData(freeSql);

        // 审计数据, _ 是 update,insert对象
        freeSql.Aop.AuditValue += (_, e) => { AuditValue(e, sysUserDto); };

        // 软删除过滤器
        freeSql.GlobalFilter.ApplyOnly<IDelete>(nameof(IDelete), a => a.IsDeleted == false);

        return freeSql;
    }

    private static void AuditValue(AuditValueEventArgs e, SysUserDto sysUserDto)
    {
        if (e.Property is null) return;

        switch (e.Column.CsType)
        {
            case { } type when type == typeof(DateTime) || type == typeof(DateTime?): // 数据库时间
                if (e.Property.GetCustomAttribute<ServerTimeAttribute>(false) is not { } serverTimeAttribute) break;

                // 插入语句, 不设置该字段服务器端时间
                if (e.AuditValueType is AuditValueType.Insert && serverTimeAttribute.CanInsert == false) return;

                // 可以更新字段, 且字段值未默认值或者为null
                if (e.Value is null || (DateTime)e.Value == default || (DateTime?)e.Value == null || serverTimeAttribute.CanUpdate)
                {
                    e.Value = DateTime.Now;
                }

                break;

            case { } type when type == typeof(long): // 雪花Id
                if (e.Property.GetCustomAttribute<SnowflakeAttribute>(false) is not { } snowflakeAttribute) break;
                if (snowflakeAttribute.Enable == false) return;

                // 可以更新字段, 且字段值未默认值或者为null
                if (e.Value is null || (long)e.Value == 0 || (long?)e.Value == null)
                {
                    e.Value = YitIdHelper.NextId();
                }

                break;
        }

        if (sysUserDto.Id <= 0) return;

        if (e.AuditValueType is AuditValueType.Insert or AuditValueType.InsertOrUpdate)
        {
            switch (e.Property.Name)
            {
                case nameof(EntityBase.CreatedUserId):
                    if (e.Value is null || (long)e.Value == 0 || (long?)e.Value == null)
                        e.Value = sysUserDto.Id;

                    break;

                case nameof(EntityBase.CreatedUserName):
                    if (e.Value is null || string.IsNullOrWhiteSpace((string)e.Value))
                        e.Value = sysUserDto.UserName;

                    break;
            }
        }

        if (e.AuditValueType is AuditValueType.Update or AuditValueType.InsertOrUpdate)
        {
            e.Value = e.Property.Name switch
            {
                nameof(EntityBase.ModifiedUserId) => sysUserDto.Id,
                nameof(EntityBase.ModifiedUserName) => sysUserDto.UserName,
                _ => e.Value
            };
        }
    }

    #endregion Freesql

    #region 同步表结构

    private static void SyncStructure(IFreeSql db)
    {
        var tableTypeList = GetTableTypeList(typeof(IEntity).Assembly);

        foreach (var entityType in tableTypeList)
        {
            db.CodeFirst.SyncStructure(entityType);
        }
    }

    private static List<Type> GetTableTypeList(params Assembly[] assemblies)
    {
        var list = new List<Type>();
        foreach (var (type, tableAttribute) in from assembly in assemblies
                                               from type in assembly.GetExportedTypes() // 公共类型
                                               let tableAttribute = type.GetCustomAttribute<TableAttribute>()
                                               select (type, tableAttribute))
        {
            if (tableAttribute is null) continue;
            if (tableAttribute.DisableSyncStructure == false) list.Add(type);
        }

        return list;
    }

    #endregion 同步表结构

    #region 同步数据

    private static void SyncData(IFreeSql db)
    {
        db.Aop.AuditValue -= SyncDataAuditValue;
        db.Aop.AuditValue += SyncDataAuditValue;

        using var unitOfWork = db.CreateUnitOfWork();

        try
        {
            var (sysUserList, sysUserPostList, sysUserRoleList, sysDeptList, sysPostList, sysRoleList, sysRoleDeptList, sysRoleMenuList, sysMenuList) = CheckExcelData();

            InitEntity(db, unitOfWork, sysUserList);
            InitEntity(db, unitOfWork, sysUserPostList);
            InitEntity(db, unitOfWork, sysUserRoleList);
            InitEntity(db, unitOfWork, sysDeptList);
            InitEntity(db, unitOfWork, sysPostList);
            InitEntity(db, unitOfWork, sysRoleList);
            InitEntity(db, unitOfWork, sysMenuList);
            InitEntity(db, unitOfWork, sysRoleDeptList);
            InitEntity(db, unitOfWork, sysRoleMenuList);

            unitOfWork.Commit();
        }
        catch (Exception)
        {
            unitOfWork.Rollback();
            throw;
        }

        db.Aop.AuditValue -= SyncDataAuditValue;

        return;

        static void SyncDataAuditValue(object? sender, AuditValueEventArgs e)
        {
            e.Value = e.Property.Name switch
            {
                nameof(EntityBase.CreatedUserId) => 1,
                nameof(EntityBase.CreatedUserName) => "Admin",
                nameof(EntityBase.CreatedTime) => DateTime.Now,
                nameof(EntityBase.ModifiedUserId) => 1,
                nameof(EntityBase.ModifiedUserName) => "Admin",
                nameof(EntityBase.ModifiedTime) => DateTime.Now,
                nameof(EntityBase.IsDeleted) => false,
                nameof(EntityBase.IsEnabled) => true,
                nameof(SysUser.Password) => MD5Encrypt.Encrypt32(e.Value?.ToString() ?? "666666"),
                _ => e.Value
            };
        }
    }

    private static void InitEntity<T>(IFreeSql db, IRepositoryUnitOfWork unitOfWork, List<T> list) where T : class, IEntity, new()
    {
        var name = typeof(T).Name;

        try
        {
            using var repository = db.GetRepository<T>();
            repository.UnitOfWork = unitOfWork;
            if (typeof(T) == typeof(SysMenu)) repository.Delete(a => a.Id > 0);

            // 数据列表
            var insertList = list.Where(t => t.Id > 0).DistinctBy(t => t.Id).ToList();
            if (insertList.Count == 0) return;

            // 查询
            var insertIdList = insertList.Select(e => e.Id).ToList();
            var dbIdList = repository.Select.Where(a => insertIdList.Contains(a.Id)).ToList().Select(t => t.Id).ToList();

            // 新增
            var insertListAdd = insertList.Where(a => dbIdList.Contains(a.Id) == false).ToList();

            if (insertListAdd.Count != 0)
            {
                repository.Insert(insertListAdd);
            }
        }
        catch (Exception ex)
        {
            throw new DbException($"table: {name} sync data failed", ex);
        }
    }

    private static (
        List<SysUser> SysUserList,
        List<SysUserPost> SysUserPostList,
        List<SysUserRole> SysUserRoleList,
        List<SysDept> SysDeptList,
        List<SysPost> SysPostList,
        List<SysRole> SysRoleList,
        List<SysRoleDept> SysRoleDeptList,
        List<SysRoleMenu> SysRoleMenuList,
        List<SysMenu> SysMenuList
        ) CheckExcelData()
    {
        var dbInitExcelFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Data\\InitData.xlsx");

        if (File.Exists(dbInitExcelFilePath) == false)
        {
            return ([], [], [], [], [], [], [], [], []);
        }

        using var fileStream = File.Open(dbInitExcelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var sysUserList = fileStream.Query<SysUser>(sheetName: nameof(SysUser)).ToList();
        var sysUserPostList = fileStream.Query<SysUserPost>(sheetName: nameof(SysUserPost)).ToList();
        var sysUserRoleList = fileStream.Query<SysUserRole>(sheetName: nameof(SysUserRole)).ToList();
        var sysDeptList = fileStream.Query<SysDept>(sheetName: nameof(SysDept)).ToList();
        var sysPostList = fileStream.Query<SysPost>(sheetName: nameof(SysPost)).ToList();
        var sysRoleList = fileStream.Query<SysRole>(sheetName: nameof(SysRole)).ToList();
        var sysRoleDeptList = fileStream.Query<SysRoleDept>(sheetName: nameof(SysRoleDept)).ToList();
        var sysRoleMenuList = fileStream.Query<SysRoleMenu>(sheetName: nameof(SysRoleMenu)).ToList();
        var sysMenuList = fileStream.Query<SysMenu>(sheetName: nameof(SysMenu)).ToList();

        return (sysUserList, sysUserPostList, sysUserRoleList, sysDeptList, sysPostList, sysRoleList, sysRoleDeptList, sysRoleMenuList, sysMenuList);
    }

    #endregion 同步数据
}

file readonly record struct LogProvider;