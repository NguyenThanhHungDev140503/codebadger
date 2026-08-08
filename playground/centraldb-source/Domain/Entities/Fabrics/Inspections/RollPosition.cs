using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Fabrics.Inspections
{
    public enum RollPosition : byte
    {
        [Display(Name = "-- Roll Position --")] 
        Unknown = 0,
        [Display(Name = "Beginning")] 
        Beginning = 1,
        [Display(Name = "Middle")] 
        Middle = 2,
        [Display(Name = "End")] 
        End = 3
    }
}
