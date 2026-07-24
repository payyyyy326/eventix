using Eventix.Share.Category;
using System.ComponentModel.DataAnnotations;

namespace Eventix.Web.Models.EventWizard
{
    public class EventInfoViewModel
    {
        [Required(ErrorMessage = "Please select a category.")]
        public Guid? CategoryId { get; set; }

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

        /// <summary>
        /// Thời điểm sự kiện được hiển thị công khai.
        /// Null = xuất bản ngay lập tức.
        /// </summary>
        public DateTime? PublishedAt { get; set; }
    }
}