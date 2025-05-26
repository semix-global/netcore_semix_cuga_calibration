using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base;

namespace Local.SQL.DB.Providers.Models.Entities
{
    /// <summary>
    /// 配方信息表
    /// </summary>
    [Table(Name = nameof(SysRecipeInformation))]
    [Index($"idx_{nameof(SysRecipeInformation)}_{nameof(RecipeDbName)}", nameof(RecipeDbName), true)]
    public sealed class SysRecipeInformation : EntityBase
    {
        /// <summary>
        /// 配方数据库名称
        /// </summary>
        [Column(IsNullable = false, StringLength = 64)]
        public string RecipeDbName { get; set; } = string.Empty;

        /// <summary>
        /// 描述信息
        /// </summary>
        [Column(IsNullable = false, StringLength = 64)]
        public string DescribeInformation { get; set; } = string.Empty;

        /// <summary>
        /// 配方Nosql数据库链接字符串
        /// </summary>
        [Column(IsNullable = false, StringLength = 64)]
        public string RecipeNosqlRecipeDbDataSource { get; set; } = string.Empty;
    }
}