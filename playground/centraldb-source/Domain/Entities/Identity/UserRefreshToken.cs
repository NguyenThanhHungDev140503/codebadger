using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Identity
{
    public class UserRefreshToken : BaseEntity<int>
    {
        public int UserId { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public required string Jti { get; set; }

        [ForeignKey(nameof(UserId))]
        public AppUser? User { get; set; }
    }
}
