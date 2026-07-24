using JobNow.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;

namespace JobNow.Controllers
{
    [Authorize]
    public class JobController : Controller
    {
        private readonly Supabase.Client _supabase;

        public JobController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        // Nhận tham số keyword và location từ thanh tìm kiếm
        public async Task<IActionResult> Index(string keyword, string location, int page = 1)
        {
            int pageSize = 5; // Số công việc trên 1 trang
            try
            {
                var response = await _supabase.From<Job>().Get();
                var jobs = response.Models;

                // Lọc dữ liệu theo từ khóa
                if (!string.IsNullOrEmpty(keyword))
                {
                    jobs = jobs.Where(j =>
                        (j.Title != null && j.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (j.CompanyName != null && j.CompanyName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // Lọc theo địa điểm
                if (!string.IsNullOrEmpty(location))
                {
                    jobs = jobs.Where(j => j.Location != null && j.Location.Contains(location, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // --- CODE PHÂN TRANG TỐI ƯU ---
                ViewBag.Keyword = keyword;
                ViewBag.Location = location;
                ViewBag.TotalJobs = jobs.Count;

                // Tính tổng số trang (Ví dụ: 15 job / 5 = 3 trang)
                ViewBag.TotalPages = (int)Math.Ceiling(jobs.Count / (double)pageSize);
                ViewBag.CurrentPage = page;

                // Cắt dữ liệu cho trang hiện tại (Skip & Take)
                var pagedJobs = jobs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                return View(pagedJobs);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(new System.Collections.Generic.List<Job>());
            }
        }
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Lấy công việc theo ID
                var response = await _supabase.From<Job>().Where(x => x.Id == id).Single();
                if (response == null) return NotFound();

                // Tiện tay lấy luôn 2 công việc ngẫu nhiên để làm phần "Việc làm liên quan" ở cuối trang
                var relatedResponse = await _supabase.From<Job>().Where(x => x.Id != id).Limit(2).Get();
                ViewBag.RelatedJobs = relatedResponse.Models;

                return View(response);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return RedirectToAction("Index"); // Nếu lỗi thì quay về trang tìm kiếm
            }
        }
    }
    
}