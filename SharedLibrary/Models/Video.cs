using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SharedLibrary.Models
{
#pragma warning disable
    public class Video
    {
        [Required]
        [Key]
        public int Id { get; set; }

        [Required]
        public virtual Movie Movie { get; set; }

        public string? VideoUrl { get; set; }

        public int? Episode { get; set; }
        [DisplayName("Thời lượng video")]
        public int? Length { get; set; }
    }
}
