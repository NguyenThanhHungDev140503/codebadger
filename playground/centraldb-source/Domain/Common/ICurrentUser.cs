namespace Domain.Common
{
    public interface ICurrentUser
    {
        int Id { get; }
        string Jti { get; }
        string UserName { get; }
        int CompanyId { get; }
        bool IsAuthenticated { get; }
        List<int> GetPermissions();
        string? IPAddress { get; set; }
        Dictionary<string, string?> GetConfigItems();
        int LanguageId { get; set; }
        DateTime? TokenExpiresAtUtc { get; }
    }
}
