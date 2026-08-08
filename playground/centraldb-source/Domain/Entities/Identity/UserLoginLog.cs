using Domain.Common;

namespace Domain.Entities.Identity
{
    public class UserLoginLog : BaseEntity<int>, IOrgEntity
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int CompanyId { get; set; }
        public bool Success { get; set; }
        public string? PasswordEncrypt { get; set; }
        public string? Password { get; set; }
        public required DateTime DateLogin { get; set; }
        public string? Ip { get; set; }
        public byte SubSystem { get; set; }
    }
}
