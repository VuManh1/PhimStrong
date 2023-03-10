using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models
{
    public class User : IdentityUser
    {
#pragma warning disable
        public User() : base() { }

        public string? DisplayName { get; set; }
        public string? NormalizeDisplayName { get; set; }

		[NotMapped]
        public IFormFile AvatarFile { get; set; }
        public string? Avatar { get; set; }

        public string? FavoriteMovie { get; set; }
        public string? Hobby { get; set; }

        public string? RoleName { get; set; }

        public virtual List<Movie>? LikedMovies { get; set; }
    }
}
