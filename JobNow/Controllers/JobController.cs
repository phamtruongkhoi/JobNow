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
                var jobs = response.Models.Where(j => !j.ExpiredAt.HasValue || j.ExpiredAt.Value > DateTime.UtcNow).ToList();

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

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        var savedJobsRes = await _supabase.From<SavedJob>().Where(x => x.ProfileId == userId).Get();
                        if (savedJobsRes.Models != null)
                        {
                            ViewBag.SavedJobIds = savedJobsRes.Models.Select(s => s.JobId).ToList();
                        }
                    }
                    catch { }
                }

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
                            .Filter("job_id", Postgrest.Constants.Operator.Equals, id)
                            .Filter("profile_id", Postgrest.Constants.Operator.Equals, userId)
                            .Get();
                        ViewBag.HasApplied = appResponse.Models != null && appResponse.Models.Any();
                    } 
                    catch { ViewBag.HasApplied = false; }

                    try 
                    {
                        var savedResponse = await _supabase.From<SavedJob>()
                            .Filter("job_id", Postgrest.Constants.Operator.Equals, id)
                            .Filter("profile_id", Postgrest.Constants.Operator.Equals, userId)
                            .Get();
                        ViewBag.IsSaved = savedResponse.Models != null && savedResponse.Models.Any();
                    } 
                    catch { ViewBag.IsSaved = false; }
                }
                else 
                {
                    ViewBag.HasApplied = false;
                    ViewBag.IsSaved = false;
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
        public async Task<IActionResult> ApplyPost(int id, int? cvId, string coverLetter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

            try
            {
                var existingAppResponse = await _supabase.From<Application>()
                    .Filter("job_id", Postgrest.Constants.Operator.Equals, id)
                    .Filter("profile_id", Postgrest.Constants.Operator.Equals, userId)
                    .Get();

                if (existingAppResponse.Models != null && existingAppResponse.Models.Any())
                {
                    TempData["Error"] = "Bạn đã ứng tuyển công việc này rồi.";
                    return RedirectToAction("Details", "Job", new { id = id });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi kiểm tra hồ sơ: " + ex.Message;
                return RedirectToAction("Details", "Job", new { id = id });
            }

            try 
            {
                var application = new Application
                {
                    JobId = id,
                    ProfileId = userId,
                    CvId = cvId,
                    CoverLetter = coverLetter,
                    Status = "Applied",
                    AppliedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _supabase.From<Application>().Insert(application);

                var job = await _supabase.From<Job>().Where(j => j.Id == id).Single();
                if (job != null)
                {
                    job.AppliedCount += 1;
                    await _supabase.From<Job>().Update(job);
                }

                TempData["Success"] = "Ứng tuyển thành công!";
                return RedirectToAction("Details", "Job", new { id = id });
            }
            catch (Postgrest.Exceptions.PostgrestException ex) when (ex.Message.Contains("23505") || ex.Message.Contains("uq_application"))
            {
                TempData["Error"] = "Bạn đã ứng tuyển công việc này rồi.";
                return RedirectToAction("Details", "Job", new { id = id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSaveJob(int jobId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để lưu công việc." });
            }

            try
            {
                var existingResponse = await _supabase.From<SavedJob>()
                    .Select("id")
                    .Filter("profile_id", Postgrest.Constants.Operator.Equals, userId)
                    .Filter("job_id", Postgrest.Constants.Operator.Equals, jobId)
                    .Get();

                if (existingResponse.Models != null && existingResponse.Models.Any())
                {
                    var savedJob = existingResponse.Models.First();
                    await _supabase.From<SavedJob>().Where(x => x.Id == savedJob.Id).Delete();
                    return Json(new { success = true, isSaved = false, message = "Đã bỏ lưu công việc." });
                }
                else
                {
                    var newSavedJob = new SavedJob
                    {
                        ProfileId = userId,
                        JobId = jobId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _supabase.From<SavedJob>().Insert(newSavedJob);
                    return Json(new { success = true, isSaved = true, message = "Đã lưu công việc." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SavedJobs()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

            try
            {
                var response = await _supabase.From<SavedJob>()
                    .Where(x => x.ProfileId == userId)
                    .Get();
                
                var savedJobs = response.Models ?? new List<SavedJob>();
                
                // Fetch the actual jobs since inner join might not map Job automatically depending on config
                // We'll just fetch all jobs that are in the saved list
                var jobIds = savedJobs.Select(s => s.JobId).ToList();
                var jobsList = new List<Job>();

                if (jobIds.Any())
                {
                    var jobsResponse = await _supabase.From<Job>().Get();
                    jobsList = jobsResponse.Models.Where(j => jobIds.Contains(j.Id)).ToList();
                }

                return View(jobsList);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải danh sách công việc đã lưu: " + ex.Message;
                return View(new List<Job>());
            }
        }
    }
}