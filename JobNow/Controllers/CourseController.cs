using JobNow.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;

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

                return View(course);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi kết nối khi tải chi tiết khóa học: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}