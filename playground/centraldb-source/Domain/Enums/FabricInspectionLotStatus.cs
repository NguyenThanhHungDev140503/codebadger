namespace Domain.Enums
{
    /// <summary>
    /// Trạng thái của lô kiểm tra vải, dùng để theo dõi tiến trình và kết quả của quá trình kiểm tra vải.
    /// </summary>
    public enum FabricInspectionLotStatus : byte
    {
        Unknown = 0,
        Waiting = 2,
        Accepted = 8,
        Approved = 16,
    }
}
