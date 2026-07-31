using JobNow.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security.Claims;
using JobNow.ViewModels;

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

        public async Task<IActionResult> Index(
            string keyword,
            string location,
            List<string> experience,
            List<string> jobType,
            long? minSalary, // Đổi thành long để chứa được tiền chục triệu VNĐ
            long? maxSalary,
            string sortOrder, // Tham số mới cho Sắp xếp
            int page = 1)
        {
            int pageSize = 5;
            try
            {
                var response = await _supabase.From<Job>().Where(x => x.Status == "Published").Get();
                var jobs = response.Models;

                // --- GỢI Ý ĐỊA ĐIỂM ---
                // Lấy danh sách các thành phố độc nhất từ database đẩy ra View
                ViewBag.Locations = jobs.Where(j => !string.IsNullOrEmpty(j.Location))
                                        .Select(j => j.Location.Trim())
                                        .Distinct()
                                        .ToList();

                // 1. Lọc theo từ khóa
                if (!string.IsNullOrEmpty(keyword))
                {
                    jobs = jobs.Where(j =>
                        (j.Title != null && j.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (j.CompanyName != null && j.CompanyName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // 2. Lọc theo địa điểm
                if (!string.IsNullOrEmpty(location))
                {
                    jobs = jobs.Where(j => j.Location != null && j.Location.Contains(location, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // 3. Lọc theo Kinh nghiệm
                if (experience != null && experience.Any())
                {
                    jobs = jobs.Where(j => j.Experience != null && experience.Contains(j.Experience)).ToList();
                }

                // 4. Lọc theo Loại công việc
                if (jobType != null && jobType.Any())
                {
                    jobs = jobs.Where(j => j.JobType != null && jobType.Contains(j.JobType)).ToList();
                }

                // 5. Lọc theo Mức lương VNĐ
                if (minSalary.HasValue || maxSalary.HasValue)
                {
                    jobs = jobs.Where(j =>
                    {
                        if (string.IsNullOrWhiteSpace(j.Salary)) return false;
                        var match = Regex.Match(j.Salary.Replace(",", "").Replace(".", ""), @"\d+");
                        if (match.Success && long.TryParse(match.Value, out long priceVal))
                        {
                            bool passMin = !minSalary.HasValue || priceVal >= minSalary.Value;
                            bool passMax = !maxSalary.HasValue || priceVal <= maxSalary.Value;
                            return passMin && passMax;
                        }
                        return false;
                    }).ToList();
                }

                // 6. XỬ LÝ SẮP XẾP (SORT)
                if (sortOrder == "salary_desc")
                {
                    // Sắp xếp lương cao nhất (Lấy con số trong chuỗi lương để so sánh)
                    jobs = jobs.OrderByDescending(j => {
                        var match = Regex.Match(j.Salary?.Replace(",", "").Replace(".", "") ?? "0", @"\d+");
                        return match.Success ? long.Parse(match.Value) : 0;
                    }).ToList();
                }
                else
                {
                    // Mặc định: Mới nhất (Dựa vào ID lớn nhất là mới nhất)
                    jobs = jobs.OrderByDescending(j => j.Id).ToList();
                }

                // --- LƯU TRẠNG THÁI BỘ LỌC ---
                ViewBag.Keyword = keyword;
                ViewBag.Location = location;
                ViewBag.SelectedExperience = experience ?? new List<string>();
                ViewBag.SelectedJobType = jobType ?? new List<string>();
                ViewBag.MinSalary = minSalary;
                ViewBag.MaxSalary = maxSalary;
                ViewBag.SortOrder = sortOrder; // Lưu trạng thái Sort

                ViewBag.TotalJobs = jobs.Count;
                ViewBag.TotalPages = (int)Math.Ceiling(jobs.Count / (double)pageSize);
                ViewBag.CurrentPage = page;

                var pagedJobs = jobs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                return View(pagedJobs);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(new List<Job>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var response = await _supabase.From<Job>().Where(x => x.Id == id && x.Status == "Published").Single();
                if (response == null) return NotFound();

                var relatedResponse = await _supabase.From<Job>().Where(x => x.Id != id && x.Status == "Published").Limit(2).Get();
                ViewBag.RelatedJobs = relatedResponse.Models;

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    try 
                    {
                        var appResponse = await _supabase.From<Application>()
                            .Where(a => a.JobId == id && a.ProfileId == userId)
                            .Single();
                        ViewBag.HasApplied = (appResponse != null);
                    } 
                    catch { ViewBag.HasApplied = false; }
                }
                else 
                {
                    ViewBag.HasApplied = false;
                }

                return View(response);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Apply(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

            var job = await _supabase.From<Job>().Where(x => x.Id == id).Single();
            if (job == null) return NotFound();

            var cvsResponse = await _supabase.From<UserCV>().Where(x => x.ProfileId == userId).Get();
            var cvs = cvsResponse.Models;

            var viewModel = new ApplyJobViewModel
            {
                Job = job,
                AvailableCVs = cvs
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ApplyPost(int jobId, int? cvId, string coverLetter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

            try
            {
                var existingApp = await _supabase.From<Application>()
                    .Where(a => a.JobId == jobId && a.ProfileId == userId)
                    .Single();

                if (existingApp != null)
                {
                    TempData["Error"] = "Bạn đã ứng tuyển công việc này rồi.";
                    return RedirectToAction("Details", new { id = jobId });
                }
            }
            catch { /* Ignored, means not found */ }

            var application = new Application
            {
                JobId = jobId,
                ProfileId = userId,
                CvId = cvId,
                CoverLetter = coverLetter,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _supabase.From<Application>().Insert(application);
            TempData["Success"] = "Ứng tuyển thành công!";
            return RedirectToAction("Details", new { id = jobId });
        }
    }
}