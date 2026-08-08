using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Fabrics.Inspections
{
    /// <summary>
    /// Loại tiêu chí kiểm tra vải, dùng để phân biệt các loại tiêu chí khác nhau trong quá trình kiểm tra vải.
    /// Ví dụ: Trọng lượng, Co rút, Xéo vải, Phai màu, In ấn, Xoắn vải, Chăn, Bảng màu, Mẫu vải,...
    /// Các loại tiêu chí này có thể có các thuộc tính và phương thức riêng biệt tùy theo yêu cầu của từng loại kiểm tra.
    /// </summary>
    public enum FabricInspectionCriteriaType : byte
    {
        [Display(Order = 0)]
        Unknown = 0,

        [Display(Order = 1, Name = "Weight")]
        Weight = 1,

        [Display(Order = 2, Name = "Shrinkage")]
        Shrinkage = 2,

        [Display(Order = 3, Name = "Skewing")]
        Skewing = 3,

        [Display(Order = 4, Name = "Crocking")]
        Crocking = 4,

        [Display(Order = 5, Name = "Color Fastness")]
        Colorfastness = 5,

        [Display(Order = 6, Name = "Printing Test")]
        Printingtest = 6,

        [Display(Order = 7, Name = "Twisting")]
        Twisting = 7,

        [Display(Order = 8, Name = "Blanket")]
        Blanket = 8,

        [Display(Order = 9, Name = "Color/Shadeband")]
        ColorShadeband = 9,

        [Display(Order = 10, Name = "Swatch")]
        Swatch = 10,
    }
}
