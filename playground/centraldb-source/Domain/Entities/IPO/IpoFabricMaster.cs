using Domain.Common;
using Domain.Entities.Accountings;
using Domain.Entities.CRM;
using Domain.Entities.Identity;
using Domain.Enums;
using Domain.Shared.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.IPO
{
    public class IpoFabricMaster : BaseOrgAuditEntity<int>
    {
        public required string MasterPOCode { get; set; }
        public string? IPOFabricItemCodeCombined { get; set; }
        public DateTime? CombinedDate { get; set; }
        public WorkTypes? WorkType { get; set; }
        public string? SupplierPartnerIds { get; set; }
        public int? CustomerPartnerId { get; set; }
        public int? CombinedByUserId { get; set; }
        public int? CurrencyId { get; set; }
        public int? PartnerId { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? DepositDate { get; set; }
        public IpoMasterStatus? IPOFabricMasterStatus { get; set; }
        [NotMapped]
        public POType? POType { get; set; }
        [NotMapped]
        public string? WorkTypeName => WorkType?.GetDisplayName();
        [NotMapped]
        public string? POTypeName => POType.GetDisplayName();
        [NotMapped]
        public string? IpoFabricMasterStatusName => IPOFabricMasterStatus.GetDisplayName();

        [ForeignKey(nameof(PartnerId))] public Partner? Supplier { get; set; }
        [ForeignKey(nameof(CustomerPartnerId))] public Partner? Customer { get; set; }
        [ForeignKey(nameof(CombinedByUserId))] public AppUser? Buyer { get; set; }
        [ForeignKey(nameof(CreatorId))] public AppUser? CreatorUser { get; set; }
        [ForeignKey(nameof(ModifierId))] public AppUser? ModifierUser { get; set; }
        [ForeignKey(nameof(CurrencyId))] public AppCurrency? Currency { get; set; }

        public ICollection<IpoFabricMasterGroup> Groups { get; set; } = new List<IpoFabricMasterGroup>();
    }
}
