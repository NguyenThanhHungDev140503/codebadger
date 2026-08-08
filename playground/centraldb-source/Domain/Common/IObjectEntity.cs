namespace Domain.Common
{
    public interface IObjectEntity
    {
        int ObjectId { get; set; }
        int ObjectType { get; set; }
    }
}
