using JobNow.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JobNow.Controllers
{
    [Authorize]
    public class CourseController : Controller
    {
        private readonly Supabase.Client _supabase;

        public CourseController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<IActionResult> Index(string keyword, string location, int? minPrice, int? maxPrice, string format, int page = 1)
        {
            int pageSize = 5;
            try
            {
                var response = await _supabase.From<Course>().Get();
                var courses = response.Models;

                // 1. Lọc theo từ khóa (Tên khóa học hoặc Tên trung tâm)
                if (!string.IsNullOrEmpty(keyword))
                {
                    courses = courses.Where(c =>
                        (c.Title != null && c.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (c.ProviderName != null && c.ProviderName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // 2. Lọc theo địa điểm
                if (!string.IsNullOrEmpty(location))
                {
                    courses = courses.Where(c => c.Location != null && c.Location.Contains(location, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // 3. Lọc theo khoảng giá
                if (minPrice.HasValue || maxPrice.HasValue)
                {
                    courses = courses.Where(c =>
                    {
                        if (int.TryParse(c.Price, out int priceVal))
                        {
                            bool passMin = !minPrice.HasValue || priceVal >= minPrice.Value;
                            bool passMax = !maxPrice.HasValue || priceVal <= maxPrice.Value;
                            return passMin && passMax;
                        }
                        return false; // Bỏ qua nếu không parse được giá
                    }).ToList();
                }

                // Lưu trạng thái để hiển thị lại trên thanh tìm kiếm / bộ lọc
                ViewBag.Keyword = keyword;
                ViewBag.Location = location;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.TotalCourses = courses.Count;

                var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        var savedCoursesRes = await _supabase.From<SavedCourse>().Where(x => x.ProfileId == userId).Get();
                        if (savedCoursesRes.Models != null)
                        {
                            ViewBag.SavedCourseIds = savedCoursesRes.Models.Select(s => s.CourseId).ToList();
                        }
                    }
                    catch { }
                }

                // Phân trang
                ViewBag.TotalPages = (int)Math.Ceiling(courses.Count / (double)pageSize);
                ViewBag.CurrentPage = page;

                var pagedCourses = courses.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                return View(pagedCourses);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(new System.Collections.Generic.List<Course>());
            }
        }
        // Thêm hàm này vào dưới hàm Index()
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                // Chọc xuống Supabase lấy đúng khóa học theo ID
                var course = await _supabase.From<Course>().Where(c => c.Id == id).Single();

                if (course == null)
                {
                    TempData["Error"] = "Không tìm thấy khóa học này hoặc đã bị xóa.";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    try 
                    {
                        var savedResponse = await _supabase.From<SavedCourse>()
                            .Filter("course_id", Postgrest.Constants.Operator.Equals, id)
                            .Filter("profile_id", Postgrest.Constants.Operator.Equals, userId)
                            .Get();
                        ViewBag.IsSaved = savedResponse.Models != null && savedResponse.Models.Any();
                    } 
                    catch { ViewBag.IsSaved = false; }
                }
                else 
                {
                    ViewBag.IsSaved = false;
                }

                return View(course);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi kết nối khi tải chi tiết khóa học: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSaveCourse(int courseId)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để lưu khóa học." });
            }

            try
            {
                var existingResponse = await _supabase.From<SavedCourse>()
                    .Select("id")
                    .Filter("profile_id", Postgrest.Constants.Operator.Equals, userId)
                    .Filter("course_id", Postgrest.Constants.Operator.Equals, courseId)
                    .Get();

                if (existingResponse.Models != null && existingResponse.Models.Any())
                {
                    var savedCourse = existingResponse.Models.First();
                    await _supabase.From<SavedCourse>().Where(x => x.Id == savedCourse.Id).Delete();
                    return Json(new { success = true, isSaved = false, message = "Đã bỏ lưu khóa học." });
                }
                else
                {
                    var newSavedCourse = new SavedCourse
                    {
                        ProfileId = userId,
                        CourseId = courseId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _supabase.From<SavedCourse>().Insert(newSavedCourse);
                    return Json(new { success = true, isSaved = true, message = "Đã lưu khóa học." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SavedCourses()
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

            try
            {
                var response = await _supabase.From<SavedCourse>()
                    .Where(x => x.ProfileId == userId)
                    .Get();
                
                var savedCourses = response.Models ?? new System.Collections.Generic.List<SavedCourse>();
                Console.WriteLine($"[DEBUG-SavedCourses] Number of SavedCourse records returned: {savedCourses.Count}");
                
                var courseIds = savedCourses.Select(s => s.CourseId).ToList();
                Console.WriteLine($"[DEBUG-SavedCourses] All CourseIds returned: {string.Join(", ", courseIds)}");
                
                var coursesList = new System.Collections.Generic.List<Course>();

                if (courseIds.Any())
                {
                    var coursesResponse = await _supabase.From<Course>().Get();
                    var allCourses = coursesResponse.Models ?? new System.Collections.Generic.List<Course>();
                    Console.WriteLine($"[DEBUG-SavedCourses] Number of Course records loaded from jn_courses: {allCourses.Count}");
                    
                    coursesList = allCourses.Where(c => courseIds.Contains(c.Id)).ToList();
                }

                Console.WriteLine($"[DEBUG-SavedCourses] Number of final courses passed to the View: {coursesList.Count}");
                return View(coursesList);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải danh sách khóa học đã lưu: " + ex.Message;
                return View(new System.Collections.Generic.List<Course>());
            }
        }
        [HttpPost]
        public async Task<IActionResult> TrackClick(int id)
        {
            try
            {
                var response = await _supabase.From<Course>().Where(c => c.Id == id).Get();
                var course = response.Models.FirstOrDefault();
                
                if (course != null)
                {
                    course.ClickCount += 1;
                    await _supabase.From<Course>().Update(course);
                    return Json(new { success = true });
                }
            }
            catch (Exception)
            {
                // Silent catch
            }
            
            return Json(new { success = false });
        }
    }
}