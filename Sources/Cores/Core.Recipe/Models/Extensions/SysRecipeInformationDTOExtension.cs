using CommunityToolkit.Diagnostics;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Net.Utilities.Helpers.Helpers.Files;
using System.IO;

namespace Core.Recipe.Models.Extensions;

public static class SysRecipeInformationDTOExtension
{
    /// <param name="sysRecipeInformationDto">admin.db管理的配方信息 DTO</param>
    extension(SysRecipeInformationDto sysRecipeInformationDto)
    {
        public string GetRecipeFolderPath()
        {
            var recipeDatabaseSourcePath = SQLiteHelper.GetDatabaseSourcePath(sysRecipeInformationDto.RecipeNosqlRecipeDbDataSource);

            if (string.IsNullOrWhiteSpace(recipeDatabaseSourcePath))
                throw new ArgumentNullException(nameof(recipeDatabaseSourcePath), "Database file path cannot be null or empty.");

            var directoryName = Path.GetDirectoryName(recipeDatabaseSourcePath);
            if (directoryName == null)
                ThrowHelper.ThrowArgumentNullException("Invalid database file path: " + recipeDatabaseSourcePath);

            return directoryName;
        }

        /// <summary>
        /// 验证配方名称（RecipeDbName）与连接字符串（RecipeNosqlRecipeDbDataSource）中内嵌的文件夹名称是否一致。
        /// </summary>
        /// <returns>一致时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public bool IsRecipeNameConsistentWithConnectionString()
        {
            if (string.IsNullOrWhiteSpace(sysRecipeInformationDto.RecipeDbName))
                return false;

            if (string.IsNullOrWhiteSpace(sysRecipeInformationDto.RecipeNosqlRecipeDbDataSource))
                return false;

            var dbFilePath = SQLiteHelper.GetDatabaseSourcePath(sysRecipeInformationDto.RecipeNosqlRecipeDbDataSource);
            if (string.IsNullOrWhiteSpace(dbFilePath))
                return false;

            // 从路径中取倒数第二段（配方文件夹名），与 RecipeDbName 做纯字符串比较
            var recipeFolder = Path.GetFileName(Path.GetDirectoryName(dbFilePath));
            if (string.IsNullOrWhiteSpace(recipeFolder)) return false;

            return string.Equals(recipeFolder, sysRecipeInformationDto.RecipeDbName, StringComparison.OrdinalIgnoreCase);
        }
    }
}