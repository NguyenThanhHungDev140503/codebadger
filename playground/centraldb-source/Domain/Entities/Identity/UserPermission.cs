using Domain.Common;

namespace Domain.Entities.Identity
{
    public class UserPermission : BaseEntity<int>
    {
        public int? PermissionId { get; set; }
        public required string PermissionIds { get; set; }
    }
}
