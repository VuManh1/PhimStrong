using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhimStrong.Data;
using SharedLibrary.Constants;
using SharedLibrary.Helpers;
using SharedLibrary.Models;
using System.Data;
using System.Text.RegularExpressions;

namespace PhimStrong.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{RoleConstant.ADMIN}, {RoleConstant.THUY_TO}")]
    public class DirectorController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public DirectorController(AppDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        private const int DIRECTOR_PER_PAGE = 15;

        [HttpGet]
        public IActionResult Index(int page, string? filter = null)
        {
            if (page <= 0) page = 1;

            int numberOfPages = 0;

            List<Director> directors = new List<Director>();
			int count = 0; // total of search result
			if (filter == null || filter.Trim() == "")
            {
				count = _db.Directors.Count();

				numberOfPages = (int)Math.Ceiling((double)count / DIRECTOR_PER_PAGE);
				TempData["TotalCount"] = count;

				if (page > numberOfPages) page = numberOfPages;
                if (page <= 0) page = 1;

				directors = _db.Directors.Skip((page - 1) * DIRECTOR_PER_PAGE).Take(DIRECTOR_PER_PAGE).ToList();
            }
            else
            {
                MatchCollection match = Regex.Matches(filter ?? "", @"^<.+>");

                if (match.Count > 0)
                {
                    string matchValue = new Regex(@"<|>").Replace(match[0].ToString(), "");

                    string filterValue = new Regex(@"^<.+>").Replace(filter ?? "", "");
                    switch (matchValue)
                    {
                        case PageFilterConstant.FILTER_BY_NAME:
                            TempData["FilterMessage"] = "tên là " + filterValue;
                            filterValue = filterValue.RemoveMarks();

                            directors = _db.Directors.ToList().Where(m =>
                                (m.NormalizeName ?? "").Contains(filterValue)
                            ).ToList();

                            break;
                        default:
                            break;
                    }
                }

                count = directors.Count;
				TempData["TotalCount"] = count;

				numberOfPages = (int)Math.Ceiling((double)count / DIRECTOR_PER_PAGE);
                if (page > numberOfPages) page = numberOfPages;
                if (page <= 0) page = 1;

                directors = directors.Skip((page - 1) * DIRECTOR_PER_PAGE).Take(DIRECTOR_PER_PAGE).ToList();
            }

            TempData["NumberOfPages"] = numberOfPages;
            TempData["CurrentPage"] = page;
            TempData["filter"] = filter;

            return View(directors);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Director());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Director director)
        {
            if (director == null)
            {
                TempData["status"] = "Lỗi, không có đạo diễn được chọn.";
                return View(director);
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Lỗi không thể thêm đạo diễn.");
                return View();
            }

            director.IdNumber = _db.Directors.Any() ? _db.Directors.Max(x => x.IdNumber) + 1 : 1;
            director.Id = "director" + director.IdNumber.ToString();

            // chỉnh lại format tên :
            director.Name = Regex.Replace(director.Name.ToLower().Trim(), @"(^\w)|(\s\w)", m => m.Value.ToUpper());
            director.NormalizeName = director.Name.RemoveMarks();

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Directors.Add(director);
                await _db.SaveChangesAsync();

                if (director.AvatarFile != null)
                {
                    var file = Path.Combine(_environment.ContentRootPath, "wwwroot/src/img/DirectorAvatars", director.Id + ".jpg");

                    using (var fileStream = new FileStream(file, FileMode.Create))
                    {
                        await director.AvatarFile.CopyToAsync(fileStream);
                    }

                    director.Avatar = "/src/img/DirectorAvatars/" + director.Id + ".jpg";

                    await _db.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                TempData["status"] = "Lỗi, " + e.Message;
                return View(director);
            }

            TempData["success"] = $"Đã thêm đạo diễn {director.Name}.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(string directorid)
        {
            var director = _db.Directors.FirstOrDefault(c => c.Id == directorid);

            if (director == null)
            {
                return NotFound("Không tìm thấy đạo diễn.");
            }

            return View(director);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string directorid, Director director)
        {
            if (director == null)
            {
                return NotFound("Không tìm thấy đạo diễn.");
            }

            var directorToEdit = _db.Directors.FirstOrDefault(c => c.Id == directorid);

            if (directorToEdit == null)
            {
                return NotFound("Không tìm thấy đạo diễn.");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Lỗi không thể sửa.");
                return View(directorToEdit);
            }

            if (director.Name != directorToEdit.Name)
            {
                directorToEdit.Name = Regex.Replace(director.Name.ToLower().Trim(), @"(^\w)|(\s\w)", m => m.Value.ToUpper());
                directorToEdit.NormalizeName = directorToEdit.Name.RemoveMarks();
			}

            if(director.About != directorToEdit.About) directorToEdit.About = director.About;
            if(director.DateOfBirth != directorToEdit.DateOfBirth) directorToEdit.DateOfBirth = director.DateOfBirth;
            if (director.AvatarFile != null)
            {
                var file = Path.Combine(_environment.ContentRootPath, "wwwroot/src/img/DirectorAvatars", directorToEdit.Id + ".jpg");

                using (var fileStream = new FileStream(file, FileMode.Create))
                {
                    await director.AvatarFile.CopyToAsync(fileStream);
                }

                if(directorToEdit.Avatar != "/src/img/DirectorAvatars/" + directorToEdit.Id + ".jpg")
                    directorToEdit.Avatar = "/src/img/DirectorAvatars/" + directorToEdit.Id + ".jpg";
            }

            try
            {
                _db.Directors.Update(directorToEdit);
                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                TempData["status"] = "Lỗi, " + e.Message;
                return View(director);
            }

            TempData["success"] = "Chỉnh sửa thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string directorid)
        {
            var director = _db.Directors.FirstOrDefault(c => c.Id == directorid);

            if (director == null)
            {
                return NotFound("Không tìm thấy đạo diễn.");
            }

            try
            {
                var file = Path.Combine(_environment.ContentRootPath, "wwwroot/src/img/DirectorAvatars", director.Id + ".jpg");

                FileInfo fileInfo = new(file);
                fileInfo.Delete();

                _db.Directors.Remove(director);
                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                TempData["status"] = "Lỗi, " + e.Message;
                return RedirectToAction("Edit", new { castid = directorid });
            }

            TempData["success"] = "Xóa thành công";
            return RedirectToAction("Index");
        }
    }
}

