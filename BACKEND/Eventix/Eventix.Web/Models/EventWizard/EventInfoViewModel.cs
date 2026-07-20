using Eventix.Share.Category;
using System.ComponentModel.DataAnnotations;

namespace Eventix.Web.Models.EventWizard
{
    public class EventInfoViewModel
    {
        [Required]
        public Guid CategoryId { get; set; }

        public List<CategoryResponse> Categories { get; set; } = new();

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? Summary { get; set; }
        public string? ImageUrl { get; set; }
        public string? BannerUrl { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }
    }
}