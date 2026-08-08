namespace Application.Common.Models.Externals
{
    public class PerRequest
    {
        public int UserId { get; set; }
    }

    public class PerResponse {
        public List<int> Permissions { get; set; } = [];
    }
}
