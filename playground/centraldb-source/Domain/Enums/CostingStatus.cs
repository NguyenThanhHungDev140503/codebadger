using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum CostingStatus : byte
{
    [Display(Name = "-- Status --")] Unknown = 0,
    [Display(Name = "Save For Later")] SaveForLater = 2,
    [Display(Name = "Waiting Client Review")] WaitingClientReview = 5,
    [Display(Name = "Waiting Finance Approve")] WaitingFinanceApprove = 20,
    [Display(Name = "Finance Reject")] FinanceReject = 30,
    [Display(Name = "Waiting Customer Approve")] WaitingCustomerApprove = 40,
    [Display(Name = "Canceled")] Canceled = 50,
    [Display(Name = "Finished")] Finished = 55,
}

public enum CostingCustomerStatus : byte
{
    [Display(Name = "-- Customer Status --")] None = 0,
    [Display(Name = "Canceled")] CustomerCancel = 5,
    [Display(Name = "Rejected")] CustomerReject = 10,
    [Display(Name = "Approved")] CustomerApprove = 15,
}
