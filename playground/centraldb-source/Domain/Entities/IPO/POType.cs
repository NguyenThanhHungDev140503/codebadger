using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace Domain.Entities.IPO
{
    public enum POType : byte
    {
        [Display(Name = "-- PO Type --")] Unknown = 0,
        [Display(Name = "Order")] Order = 1,
        [Display(Name = "Blanket")] Blanket = 2,
    }

    public enum IpoMasterStatus : byte
    {
        [Display(Name = "-- Master PO Status --")] Unknown = 0,
        [Display(Name = "Draft")] Draft = 1,

        [Display(Name = "Waiting Pur Manager Approval")] WaitingPurchManagerAproved = 2,
        [Display(Name = "Purch Manager Rejected")] PurchManagerRejected = 3,
        [Display(Name = "Purch Manager Accepted")] PurchManagerAcepted = 4,
        [Display(Name = "Purch Manager Approved")] PurchManagerApproved = 5,

        [Display(Name = "Removed Combine")] RemovedCombine = 6,

        [Display(Name = "Waiting Commercial Approval")] MerWaiting = 7,
        [Display(Name = "Commercial Rejected")] MerRejected = 8,
        [Display(Name = "Commercial Accepted")] MerAcepted = 9,
        [Display(Name = "Commercial Approved")] MerApproved = 10,

        [Display(Name = "Purch Processing")] PurProcessing = 11,
    }
}
