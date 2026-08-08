using Application.Features.CentralDbSync.Config;
using Application.Features.CentralDbSync.Mapping;
using static Application.Features.CentralDbSync.Config.Rules.MappingRuleCatalogFactory;

namespace Application.Features.CentralDbSync.Config.Rules.IPO.Fabric;

public sealed class IpoFabricMasterGroupReceivingMappingRuleCatalog : ITableMappingRuleCatalog
{
    public IReadOnlyList<TableMappingRule> GetRules() =>
    [
        Create(
            ruleName:           "ERP.IPO.Fabrics.Masters.Groups.Receivings",
            sourceTable:        "ERP.IPO.Fabrics.Masters.Groups.Receivings",
            sourcePrimaryKey:   "LotReceivingId",
            targetTable:        "ipo_fabrics_masters_groups_receivings",
            targetPrimaryKey:   "ipo_fabric_master_group_receiving_id",
            syncTier:           "Hot",
            filterCompany:      true,
            columns:
            [
                MapPk(  "ipo_fabric_master_group_receiving_id", "integer",  "t0.LotReceivingId"),
                Map(    "company_id",                           "integer",  "t0.CompanyId"),
                Map(    "ipo_fabric_master_group_id",           "integer",  "t0.IPOFabricMasterGroupId"),
                Map(    "lot",                                  "text",     "t0.LOT"),
                Map(    "shipping_sample_inspection",           "boolean",  "t0.ShippingSampleInspection"),
                Map(    "fail_reason",                          "text",     "t0.FailReason"),
                Map(    "object_id",                            "integer",  "t0.ObjectId"),
                Map(    "object_type",                          "integer",  "t0.ObjectType"),
                Map(    "total_roll",                           "integer",  "t0.TotalRoll"),
                Map(    "received_qty_of_supplier",             "numeric",  "t0.ReceivedQtyOfSupplier"),
                Map(    "returned_qty",                         "numeric",  "t0.ReturnedQty"),
                Map(    "location_names",                       "text",     "t0.LocationNames"),
                Map(    "location_ids", "text", "t0.LocationIds"),
                Map(    "received_units_id", "integer", "t0.UnitIdReceived"),
                Map(    "received_qty", "numeric", "t0.ReceivedQty"),
                Map(    "weight", "numeric", "t0.Weight"),
                Map(    "sent_qa", "boolean", "t0.SentQA"),
                Map(    "remark", "text", "t0.Remark"),
                Map(    "wh_received", "boolean", "t0.WHReceived"),
                Map(    "received_date", "timestamp", "t0.ReceivedDate"),
                Map(    "last_received_date", "timestamp", "t0.LastReceivedDate"),
                Map(    "sent_qa_date", "timestamp", "t0.SentQADate"),
                Map(    "return_to_supplier", "boolean", "t0.ReturnToSupplier"),
                Map(    "return_roll_to_supplier", "boolean", "t0.ReturnRollToSupplier"),
                Map(    "remain_qty_after_returned", "numeric", "t0.RemainQtyAfterReturned")
            ])
    ];
}
