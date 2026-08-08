using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.Identity;
using Domain.Entities.Trims;

namespace Domain.Entities.Fabrics;

public class FabricItem : BaseOrgAuditEntity<int>
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int KindOfFabricId { get; set; }
    public int? FabricColorId { get; set; }
    public int? FabricCompositionId { get; set; }
    public int? UnitId { get; set; }
    public bool? IsActivate { get; set; }

    [ForeignKey(nameof(UnitId))]
    public Unit? Unit { get; set; }
    
    [ForeignKey(nameof(KindOfFabricId))]
    public FabricsKinds? KindOfFabric { get; set; }

    [ForeignKey(nameof(FabricColorId))]
    public FabricColor? FabricColor { get; set; }

    [ForeignKey(nameof(FabricCompositionId))]
    public FabricComposition? FabricComposition { get; set; }

    public int? WeaveParameterId { get; set; }

    [ForeignKey(nameof(WeaveParameterId))]
    public WeaveParameter? WeaveParameter { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
}