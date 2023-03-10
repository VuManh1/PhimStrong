using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhimStrong.Data;
using PhimStrong.Models;
using SharedLibrary.Constants;
using SharedLibrary.Models;
using System.Text.Encodings.Web;

namespace PhimStrong.Controllers
{
#pragma warning disable
	public class CommentController : Controller
	{
		private readonly AppDbContext _db;
		private readonly UserManager<User> _userManager;
		private readonly SignInManager<User> _signInManager;
		private readonly IEmailSender _emailSender;

		public CommentController(
			AppDbContext db,
			UserManager<User> userManager,
			SignInManager<User> signInManager,
			IEmailSender emailSender)
		{
			_db = db;
			_userManager = userManager;
			_signInManager = signInManager;
			_emailSender = emailSender;
		}

		[HttpGet]
		[Route("/Comment/GetCommentsPartial")]
		public async Task<IActionResult> GetCommentsPartial(int page, string movieid)
		{
			Movie? movie = await _db.Movies.FirstOrDefaultAsync(m => m.Id == movieid);
			if (movie == null) return Json("null");

			if (page <= 0) page = 1;

			List<Comment>? comments = movie.Comments != null ?
				movie.Comments.Where(c => c.ResponseTo == null)
				.OrderByDescending(c => c.CreatedAt)
				.Skip((page - 1) * CommonConstants.COMMENTS_PER_PAGE)
				.Take(CommonConstants.COMMENTS_PER_PAGE).ToList() :
				null;

			User user = await _userManager.GetUserAsync(User);

			CommentModel model = new()
			{
				Comments = comments,
				UserLogin = _signInManager.IsSignedIn(User),
				CommentCount = movie.Comments != null ? movie.Comments.Count : 0,
				RenderCommentOnly = false,
				UserAvatar = user != null ? user.Avatar : null,
				MovieId = movie.Id,
				IsAdmin = user != null && user.RoleName != null && (user.RoleName == RoleConstant.ADMIN || user.RoleName == RoleConstant.THUY_TO)
			};

			return this.PartialView("_CommentPartial", model);
		}

		[HttpGet]
		[Route("/Comment/LoadMoreComments")]
		public async Task<IActionResult> LoadMoreComments(int page, string movieid)
		{
			Movie? movie = await _db.Movies.FirstOrDefaultAsync(m => m.Id == movieid);
			if (movie == null) return Json("null");

			if (page <= 0) page = 1;

            User user = await _userManager.GetUserAsync(User);

            List<Comment>? comments = movie.Comments != null ?
				movie.Comments.Where(c => c.ResponseTo == null)
				.OrderByDescending(c => c.CreatedAt)
				.Skip((page - 1) * CommonConstants.COMMENTS_PER_PAGE)
				.Take(CommonConstants.COMMENTS_PER_PAGE).ToList() :
				null;

			CommentModel model = new()
			{
				Comments = comments,
				RenderCommentOnly = true,
                IsAdmin = user != null && user.RoleName != null && (user.RoleName == RoleConstant.ADMIN || user.RoleName == RoleConstant.THUY_TO)
            };

			return this.PartialView("_CommentPartial", model);
		}

		[HttpPost]
		[Route("/Comment/CreateComment")]
		public async Task<JsonResult> CreateComment(UserCommentModel? model)
		{
			if (model == null)
			{
				return Json(new { success = false });
			}

			User user = await _userManager.GetUserAsync(User);
			Movie? movie = await _db.Movies.FirstOrDefaultAsync(m => m.Id == model.MovieId);

			if (user == null || movie == null)
			{
				return Json(new { success = false });
			}

			Comment? responseToComment = model.ResponseToId > 0 ? 
				_db.Comments.FirstOrDefault(c => c.Id == model.ResponseToId) : null;

			Comment comment = new()
			{
				User = user,
				Movie = movie,
				Content = model.Content,
				CreatedAt = DateTime.Now,
				Like = 0,
				ResponseTo = responseToComment
			};

			try
			{
				_db.Comments.Add(comment);
				await _db.SaveChangesAsync();
			}
			catch
			{
				return Json(new { success = false });
			}

			// gửi email tới user đc phàn hồi
			if (responseToComment != null)
			{
				var callbackUrl = Url.Action(
					"Detail",
					"Movie",
					values: new { area = "", id = movie.Id },
					protocol: Request.Scheme);

				_emailSender.SendEmailAsync(
					responseToComment.User.Email,
					"Thông báo",
					$"Ai đó vừa phản hồi Comment của bạn về bộ phim {movie.Name}. " +
					$"<a href='{HtmlEncoder.Default.Encode(callbackUrl ?? "")}'>Ấn vào đây</a> để đi đến trang Web PhimStrong."
				);
			}

			return Json(new
			{
				success = true,
				useravatar = user.Avatar ?? "/src/img/UserAvatars/default_avatar.png",
				username = user.DisplayName,
				cmtcontent = model.Content,
				userrole = user.RoleName ?? ""
			});
		}

		[HttpPost]
		[Route("/Comment/LikeComment")]
		public async Task<JsonResult> LikeComment(int commentid)
		{
			Comment? comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentid);

			if (comment == null)
			{
				return Json(new { success = false });
			}

			comment.Like += 1;
			try
			{
				_db.Update(comment);
				await _db.SaveChangesAsync();
			}
			catch
			{
				return Json(new { success = false });
			}

			return Json(new {success = true});
		}

        [HttpPost]
        [Route("/Comment/DeleteComment")]
		[Authorize(Roles = $"{RoleConstant.ADMIN}, {RoleConstant.THUY_TO}")]
		public async Task<JsonResult> DeleteComment(int commentid)
        {
            Comment? comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentid);

            if (comment == null)
            {
                return Json(new { success = false });
            }

            try
            {
				if (comment.Responses != null && comment.Responses.Count > 0)
				{
					foreach (var cmt in comment.Responses)
					{
						_db.Comments.Remove(cmt);
					}
				}

                _db.Comments.Remove(comment);
                await _db.SaveChangesAsync();
            }
            catch
            {
                return Json(new { success = false });
            }

            return Json(new { success = true });
        }
    }
}
