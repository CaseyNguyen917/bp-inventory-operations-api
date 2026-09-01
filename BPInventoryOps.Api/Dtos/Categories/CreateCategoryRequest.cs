using System.ComponentModel.DataAnnotations;

namespace BPInventoryOps.Api.Dtos.Categories;

public sealed class CreateCategoryRequest
{
    [Required]
    [MaxLength(80)]
    public string Name { get; init; } = string.Empty;
}
