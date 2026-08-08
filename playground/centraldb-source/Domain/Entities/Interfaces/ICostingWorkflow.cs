namespace Domain.Entities.Interfaces;

public interface ICostingWorkflow
{
    // Merchandiser
    DateTime? MerSentDate { get; set; }
    int? MerSentById { get; set; }

    // Client
    DateTime? ClientSentDate { get; set; }
    int? ClientSentById { get; set; }
    DateTime? ClientAcceptedDate { get; set; }
    int? ClientAcceptedById { get; set; }
    DateTime? ClientRejectedDate { get; set; }
    int? ClientRejectedById { get; set; }
    string? ClientRejectReason { get; set; }

    // Finance
    DateTime? FinanceApprovedDate { get; set; }
    int? FinanceApprovedById { get; set; }
    DateTime? FinanceRejectedDate { get; set; }
    int? FinanceRejectedById { get; set; }
    string? FinanceNote { get; set; }

    // Customer
    DateTime? CustomerApprovedDate { get; set; }
    DateTime? CustomerCanceledDate { get; set; }
    string? CustomerNote { get; set; }
}
