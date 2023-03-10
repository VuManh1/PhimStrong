using Microsoft.AspNetCore.Mvc;
using PhimStrong.Data;
using SharedLibrary.Constants;
using SharedLibrary.Helpers;
using SharedLibrary.Models;
using System.IO;

namespace PhimStrong.Controllers
{
	public class CastController : Controller
	{
		private readonly AppDbContext _db;

		public CastController(AppDbContext db)
		{
			_db = db;
		}

		[HttpGet]
		[Route("/Cast/{value}")]
		public IActionResult Index(string? value, int page)
		{
			if (value == null) value = "";

			value = value.RemoveMarks();

			Cast? cast = _db.Casts.FirstOrDefault(c => c.NormalizeName == value);

			if (cast == null)
			{
				return NotFound("Không tìm thấy diễn viên " + value);
			}

			List<Movie> movies = cast.Movies != null ?
				cast.Movies.OrderByDescending(m => m.ReleaseDate).ToList() :
				new List<Movie>();

			int numberOfPages = (int)Math.Ceiling((double)movies.Count / CommonConstants.MOVIES_PER_PAGE);
			if (page > numberOfPages) page = numberOfPages;
			if (page <= 0) page = 1;

			movies = movies.Skip((page - 1) * CommonConstants.MOVIES_PER_PAGE)
				.Take(CommonConstants.MOVIES_PER_PAGE).ToList();

			TempData["NumberOfPages"] = numberOfPages;
			TempData["CurrentPage"] = page;
			TempData["RouteValue"] = value;

			TempData["Title"] = cast.Name;
			ViewData["Filter"] = "Diễn viên";
			ViewData["Description"] = cast.About;
			ViewData["Image"] = cast.Avatar;

			return View(movies);
		}
	}
}
