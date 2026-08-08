using Domain.Common;
using Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Entities.Configs;

namespace Domain.Entities.CRM
{
    public class Partner : BaseOrgAuditEntityVersion<int>
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public bool IsCustomer { get; set; }
        public bool IsSupplier { get; set; }

        public string? Phone { get; set; }
        public string? TaxCode { get; set; }

        public string? Deputy { get; set; }
        public DateTime? Anniversary { get; set; }

        public string? ContactPhone { get; set; }
        public string? Website { get; set; }
        public string? BankName { get; set; }
        public string? BankAddress { get; set; }
        public string? BankAccount { get; set; }
        public string? BankNumber { get; set; }
        public string? SwiftCode { get; set; }
        public decimal? NumberEmployee { get; set; }
        public string? DeliveryAdress { get; set; }
        public string? DeliveryWard { get; set; }
        public string? DeliveryDistrict { get; set; }
        public string? DeliveryProvince { get; set; }
        public string? DeliveryPerson { get; set; }
        public string? DeliveryPhone { get; set; }
        public string? DeliveryEmail { get; set; }
        public DateTime? CargoReady { get; set; }
        public byte? AgentType { get; set; }
        public string? Commisssion { get; set; }
        public string? PaymentTerm { get; set; }
        public string? Confirmation { get; set; }
        public string? OtherNote { get; set; }
        public byte? TypeOfEntity { get; set; }
        public string? Principal { get; set; }
        public decimal? WastagePercent { get; set; }
        public string? BrandName { get; set; }
        public int? YearFounded { get; set; }
        public string? Location { get; set; }
        public int? InstagramFollowing { get; set; }
        public decimal? Commission { get; set; }
        public decimal? ScorePoint { get; set; }
        public bool? Activated { get; set; }
        public bool? HasCreditCheck { get; set; }
        public string? ShippingManual { get; set; }
        public bool? HasInsurance { get; set; }
        public bool? HasContractAgreement { get; set; }
        public bool? HasAuditRequirement { get; set; }
        public bool? HasCalendarCadence { get; set; }
        public decimal? Deposit { get; set; }
        public decimal? AtShipment { get; set; }
        public decimal? AfterShipment { get; set; }
        public int? Shipment { get; set; }
        public decimal? ShippingToleranceMinus { get; set; }
        public decimal? ShippingTolerancePlus { get; set; }
        public string? BrandCategory { get; set; }
        public string? StoreType { get; set; }
        public int? ExpectAnnualUnits { get; set; }
        public decimal? ExpectAnnualRevenue { get; set; }
        public decimal? AverageFob { get; set; }
        public int? IntroductionYear { get; set; }
        public string? LogoPath { get; set; }

        public int? GroupId { get; set; }
        public int? SourceId { get; set; }
        public int? TypeId { get; set; }
        public int? ContinentId { get; set; }
        public int? NationalityId { get; set; }
        public int? RegionId { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? WardId { get; set; }
        public int? DeliveryId { get; set; }
        public int? QualityAgreementId { get; set; }
        public int? EnterpriseSizeId { get; set; }
        public int? CostingRateId { get; set; }

        [ForeignKey("QualityAgreementId")]
        public QualityAgreement? QualityAgreement { get; set; }

        [ForeignKey("NationalityId")]
        public Nationality? Nationality { get; set; }

        [ForeignKey("CreatorId")]
        public AppUser? CreatedByUser { get; set; }

        [ForeignKey("ModifierId")]
        public AppUser? UpdatedByUser { get; set; }

        [ForeignKey("EnterpriseSizeId")]
        public EnterpriseSize? EnterPriseSize { get; set; }

        [ForeignKey("CostingRateId")]
        public CostingRate? CostingRate { get; set; }

        [ForeignKey("DeliveryId")]
        public Delivery? Delivery { get; set; }
    }
}
