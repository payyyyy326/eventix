using Eventix.Share.Category;
using Eventix.Share.Venue;
using System.ComponentModel.DataAnnotations;

namespace Eventix.Web.Models;

public class EditEventViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Tên sự kiện là bắt buộc.")]
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn địa điểm.")]
    public Guid VenueId { get; set; }

    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? BannerUrl { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public string Status { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public DateTime? PublishedAt { get; set; }

    public List<CategoryResponse> Categories { get; set; } = [];
    public List<VenueResponse> Venues { get; set; } = [];
}