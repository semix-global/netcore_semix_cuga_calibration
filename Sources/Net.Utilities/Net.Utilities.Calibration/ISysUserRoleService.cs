using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Net.Utilities.Calibration;

public interface ISysUserRoleService
{ 
    Task<bool> InsertAsync(SysUserDTO sysUserDTO, CancellationToken cancellationToken = default);
}