namespace Domain.Enums
{
    public enum UAObjectTypes : byte
    {
        Unknown = 0,

        Company = 6,
        Partner = 7,
        OrderService = 8,

        ERP_MockupOrderQty = 13,
        ERP_MockupTreatment = 14,
        ERP_MockupFabric = 15,
        ERP_MockupTrim = 16,
        ERP_MockupCmp = 17,
        ERP_MockupPOM = 18,

        ERP_PurchaseOrder = 19,
        AccountBank = 20,
        UA_FabricSupplierItem = 21,
        UA_ImportFabricSupplier = 22,
        UA_ImportFabricSupplierDetail = 23,
        UA_ExportFabricSupplier = 24,
        UA_ExportFabricSupplierDetail = 25,
        ShippingSampleRequestDetail = 26,

        UA_RollItem = 27,
        UA_AppearanceTesting = 28,
        IPO_Fabric = 29,
        IPO_Trim = 30,
        IPO_Trim_Master_Group = 31,
        UA_Trim_StockIn = 32,
        UA_TrimItem = 33,
        FabricRequestReturnSupplier = 34,
        TagBundle = 35,
        TagGarment = 36,
    }
}
