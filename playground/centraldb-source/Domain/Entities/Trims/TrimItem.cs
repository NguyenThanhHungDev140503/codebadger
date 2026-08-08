using Domain.Common;
using Domain.Entities.Configs;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Trims;

public class TrimItem : BaseOrgAuditEntityVersion<int>
{
    public string? Code { get; set; }
    public int? KindOfTrimId { get; set; }
    public int? TypeOfTrimId { get; set; }
    public int? TrimCompositionId { get; set; }
    public int? ColorId { get; set; }
    public int? UnitId { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? DescDetail { get; set; }
    public bool? IsActivate { get; set; }
    public string? Dimension { get; set; }
    public string? PhotoFirst { get; set; }

    [ForeignKey(nameof(ColorId))]
    public Color? Color { get; set; }

    [ForeignKey(nameof(UnitId))]
    public Unit? Unit { get; set; }

    [ForeignKey(nameof(KindOfTrimId))]
    public TrimsKinds? KindOfTrim { get; set; }

    [ForeignKey(nameof(TypeOfTrimId))]
    public TrimsTypes? TypeOfTrim { get; set; }

    [ForeignKey(nameof(TrimCompositionId))]
    public TrimComposition? Composition { get; set; }
}
