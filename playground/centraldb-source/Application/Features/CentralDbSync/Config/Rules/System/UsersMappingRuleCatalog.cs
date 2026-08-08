using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.System;

public sealed class UsersMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName: "Users",
            sourceTable: "Users",
            sourcePrimaryKey: "UserId",
            targetTable: "users",
            targetPrimaryKey: "users_id",
            filterCompany: true,
            syncTier: "Hot",
            columns:
            [
                MapPk("users_id", "integer", "t0.UserId"),
                Map("user_name", "text", "t0.UserName"),
                Map("full_name", "text", "t0.FullName"),
                Map("email", "text", "t0.Email"),
                Map("company_id", "integer", "t0.CompanyId"),
                Map("is_lock", "boolean", "t0.IsLock"),
                Map("locked_by_user_id", "integer", "t0.LockedBy"),
                Map("is_admin", "boolean", "t0.IsAdmin"),
                Map("avatar", "text", "t0.Avatar"),
                Map("role_id", "integer", "t0.RoleId"),
                Map("is_boss", "boolean", "t0.IsBoss"),
                Map("user_type", "integer", "t0.UserType"),
                Map("department_id", "integer", "t0.DepartmentId"),
                Map("qr_code_path", "text", "t0.QRCodePath"),
                Map("mobile", "text", "t0.Mobile"),
                Map("version", "integer", "t0.Version"),
                Map("created_by_user_id", "integer", "t0.CreatedByUserId"),
                Map("created_date", "timestamp", "t0.CreatedDate"),
                Map("updated_by_user_id", "integer", "t0.UpdatedByUserId"),
                Map("updated_date", "timestamp", "t0.UpdatedDate")
            ])
    ];
}

