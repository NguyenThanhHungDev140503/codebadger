using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum CostingFlow : byte
{
    [Display(Name = "Create")] None = 0,
    [Display(Name = "Update")] Update = 1,
    [Display(Name = "Save For Later")] Create = 2,
    [Display(Name = "Sent To Client")] SentToClient = 5,
    [Display(Name = "Client Reject")] ClientReject = 10,
    [Display(Name = "Sent To Finance")] SentToFinance = 20,
    [Display(Name = "Finance Reject")] FinanceReject = 30,
    [Display(Name = "Finance Approved")] FinanceApproved = 40,
    [Display(Name = "Customer Cancel")] Canceled = 50,
    [Display(Name = "Customer Reject")] Reject = 52,
    [Display(Name = "Customer Approved")] Finished = 55,
    [Display(Name = "Refresh Costing Price")] RefreshPrice = 60,
}
