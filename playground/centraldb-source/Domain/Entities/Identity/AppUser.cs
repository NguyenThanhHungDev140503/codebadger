using Domain.Common;
using System.Text.Json.Serialization;

namespace Domain.Entities.Identity
{
    public class AppUser : BaseAuditEntity<int>, IOrgEntity
    {
        #region Properties
        public required string UserName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        [JsonIgnore]
        public string? Password { get; set; }
        public int CompanyId { get; set; }
        public string? PhoneNumber { get; set;}
        public bool Disabled { get; set; }
        public int? DepartmentId { get; set; }
        public int? TotalLoginFail { get; set; }
        public DateTime? LoginLastestDate { get; set; }
        public DateTime? LoginFailLastestDate { get; set; }
        public bool? FirstChangePassword { get; set; }
        public DateTime? LastChangePassword { get; set; }
        public string? PhotoPath { get; set; }
        public string? QRCodePath { get; set; }
        #endregion

        public Company? Company { get; set; }
        public Department? Department { get; set; }
        public ICollection<UserRefreshToken> UserRefreshTokens { get; set; } = [];
    }
}
