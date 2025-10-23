using System.ComponentModel.DataAnnotations;

namespace Avancira.Domain.Students;

public enum LearningStyle
{
    [Display(Name = "Visual (learns by seeing)")]
    Visual = 1,
    [Display(Name = "Auditory (learns by hearing)")]
    Auditory = 2,
    [Display(Name = "Reading/Writing")]
    ReadingWriting = 3,
    [Display(Name = "Kinesthetic (learns by doing)")]
    Kinesthetic = 4
}
