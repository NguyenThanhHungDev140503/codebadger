using Domain.Common;
using Domain.Entities.Configs;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CRM
{
    public class CustomerWarehouse : BaseOrgAuditEntityVersion<int>
    {
        public int PartnerId { get; set; }
        [Column("WarehouseName")] public string? Name { get; set; }
        [Column("WarehouseCode")] public string? Code { get; set; }
        public string? Store { get; set; }
        public string? Address { get; set; }
        public int NationalityId { get; set; }
        public string? Port { get; set; }
        public string? Contact { get; set; }
        public string? Tel { get; set; }
        public string? Mobile { get; set; }
        public string? HandleBy { get; set; }

        [ForeignKey("NationalityId")]
        public Nationality? Nationality { get; set; }

    }
}
