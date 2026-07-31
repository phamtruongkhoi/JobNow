using JobNow.Models;
using JobNow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JobNow.Controllers
{
    [Authorize]
    public class EmployerController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly IConfiguration _configuration;

        public EmployerController(Supabase.Client supabase, IConfiguration configuration)
        {
            _supabase = supabase;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var employer = await GetCurrentEmployerAsync();

                if (employer == null)
                {
                    // Chưa có hồ sơ công ty — hiển thị dashboard trống
                    ViewBag.TotalJobs = 0;
                    ViewBag.ActiveJobs = 0;
                    ViewBag.ExpiredJobs = 0;
                    ViewBag.TotalApplicants = 0;
                    ViewBag.NoEmployerProfile = true;
                    return View();
                }

                var response = await _supabase.From<Job>()
                    .Where(j => j.EmployerId == employer.Id)
                    .Get();
                var jobs = response.Models;

                ViewBag.TotalJobs = jobs.Count;
                ViewBag.ActiveJobs = jobs.Count(j => j.Status == "Published" && j.Deadline >= DateTime.Now);
                ViewBag.ExpiredJobs = jobs.Count(j => j.Deadline < DateTime.Now);
                ViewBag.TotalApplicants = jobs.Sum(j => j.AppliedCount);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.TotalJobs = 0;
                ViewBag.ActiveJobs = 0;
                ViewBag.ExpiredJobs = 0;
                ViewBag.TotalApplicants = 0;
            }

            return View();
        }

        public async Task<IActionResult> MyJobs()
        {
            try
            {
                var employer = await GetCurrentEmployerAsync();

                if (employer == null)
                {
                    ViewBag.NoEmployerProfile = true;
                    return View(new List<Job>());
                }

                var response = await _supabase.From<Job>()
                    .Where(j => j.EmployerId == employer.Id)
                    .Get();
                // Sắp xếp bài đăng mới nhất lên đầu
                var jobs = response.Models.OrderByDescending(j => j.CreatedAt).ToList();
                return View(jobs);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(new List<Job>());
            }
        }

        public IActionResult CreateJob()
        {
            return View(new CreateJobViewModel { Deadline = DateTime.Now.AddDays(30) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJobPost(CreateJobViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateJob", model);
            }

            try
            {
                // Format Salary
                string salaryStr = "Thỏa thuận";
                if (!string.IsNullOrEmpty(model.SalaryMin) || !string.IsNullOrEmpty(model.SalaryMax))
                {
                    salaryStr = $"{model.SalaryMin} - {model.SalaryMax}".Trim(new char[] { ' ', '-' });
                }

                var job = new Job
                {
                    Title = model.Title,
                    CompanyName = model.CompanyName,
                    JobType = model.JobType,
                    Experience = model.Experience,
                    Salary = salaryStr,
                    Quantity = model.Quantity,
                    Deadline = model.Deadline,
                    Location = model.Location,
                    Tags = !string.IsNullOrEmpty(model.Industry) ? $"{model.Industry}, {model.Tags}".Trim(new char[] { ',', ' ' }) : model.Tags,
                    Description = model.Description,
                    Requirements = model.Requirements,
                    Benefits = model.Benefits,
                    Status = string.IsNullOrEmpty(model.Status) ? "Draft" : model.Status,
                    PostedAt = DateTime.Now.ToString("dd/MM/yyyy"), // Giữ format cũ cho tương thích
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AppliedCount = 0,
                    IsHot = false,
                    IsNew = true,
                    EmployerId = (await GetCurrentEmployerAsync())?.Id
                };

                await _supabase.From<Job>().Insert(job);
                return RedirectToAction("MyJobs");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu tin: " + ex.Message);
                return View("CreateJob", model);
            }
        }

        public async Task<IActionResult> EditJob(int id)
        {
            try
            {
                var employer = await GetCurrentEmployerAsync();
                if (employer == null) return RedirectToAction("Index");

                var job = await _supabase.From<Job>().Where(x => x.Id == id && x.EmployerId == employer.Id).Single();
                if (job == null) return NotFound();

                // Parse lại salary (giả định format là "min - max")
                string minSal = "", maxSal = "";
                if (!string.IsNullOrEmpty(job.Salary) && job.Salary.Contains("-"))
                {
                    var parts = job.Salary.Split('-');
                    if (parts.Length == 2)
                    {
                        minSal = parts[0].Trim();
                        maxSal = parts[1].Trim();
                    }
                }
                else if (job.Salary != "Thỏa thuận")
                {
                    minSal = job.Salary;
                }

                var viewModel = new CreateJobViewModel
                {
                    Id = job.Id,
                    Title = job.Title,
                    CompanyName = job.CompanyName,
                    JobType = job.JobType,
                    Experience = job.Experience,
                    SalaryMin = minSal,
                    SalaryMax = maxSal,
                    Quantity = job.Quantity,
                    Deadline = job.Deadline,
                    Location = job.Location,
                    Tags = job.Tags, // Sẽ bao gồm cả Industry vì mình đã gộp lúc Create
                    Description = job.Description,
                    Requirements = job.Requirements,
                    Benefits = job.Benefits,
                    Status = job.Status
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return RedirectToAction("MyJobs");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJobPost(int id, CreateJobViewModel model)
        {
            if (id != model.Id || !ModelState.IsValid)
            {
                return View("EditJob", model);
            }

            try
            {
                var employer = await GetCurrentEmployerAsync();
                if (employer == null) return Unauthorized();

                var existingJob = await _supabase.From<Job>().Where(x => x.Id == id && x.EmployerId == employer.Id).Single();
                if (existingJob == null) return NotFound("Không tìm thấy tin hoặc bạn không có quyền sửa tin này.");

                // Format Salary
                string salaryStr = "Thỏa thuận";
                if (!string.IsNullOrEmpty(model.SalaryMin) || !string.IsNullOrEmpty(model.SalaryMax))
                {
                    salaryStr = $"{model.SalaryMin} - {model.SalaryMax}".Trim(new char[] { ' ', '-' });
                }

                existingJob.Title = model.Title;
                existingJob.CompanyName = model.CompanyName;
                existingJob.JobType = model.JobType;
                existingJob.Experience = model.Experience;
                existingJob.Salary = salaryStr;
                existingJob.Quantity = model.Quantity;
                existingJob.Deadline = model.Deadline;
                existingJob.Location = model.Location;
                existingJob.Tags = !string.IsNullOrEmpty(model.Industry) ? $"{model.Industry}, {model.Tags}".Trim(new char[] { ',', ' ' }) : model.Tags;
                existingJob.Description = model.Description;
                existingJob.Requirements = model.Requirements;
                existingJob.Benefits = model.Benefits;
                existingJob.Status = string.IsNullOrEmpty(model.Status) ? "Draft" : model.Status;
                existingJob.UpdatedAt = DateTime.UtcNow;

                // Supabase UPDATE by primary key
                await _supabase.From<Job>().Update(existingJob);
                return RedirectToAction("MyJobs");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật tin: " + ex.Message);
                return View("EditJob", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int id)
        {
            try
            {
                var employer = await GetCurrentEmployerAsync();
                if (employer != null)
                {
                    await _supabase.From<Job>().Where(j => j.Id == id && j.EmployerId == employer.Id).Delete();
                }
                return RedirectToAction("MyJobs");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return RedirectToAction("MyJobs");
            }
        }

        // ==========================================
        // COURSE MANAGEMENT
        // ==========================================

        public async Task<IActionResult> MyCourses()
        {
            var employer = await GetCurrentEmployerAsync();
            if (employer == null) return RedirectToAction("Index");

            try
            {
                var response = await _supabase.From<Course>().Where(c => c.EmployerId == employer.Id).Get();
                var courses = response.Models ?? new List<Course>();
                return View(courses);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(new List<Course>());
            }
        }

        public IActionResult CreateCourse()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(JobNow.ViewModels.CreateCourseViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var employer = await GetCurrentEmployerAsync();
                var course = new Course
                {
                    Title = model.Title,
                    ProviderName = model.ProviderName,
                    ProviderLogo = model.ProviderLogo,
                    Price = model.Price,
                    Location = model.Location,
                    PostedAt = DateTime.Now.ToString("dd/MM/yyyy"),
                    Deadline = model.Deadline,
                    Tags = model.Tags,
                    IsHot = false,
                    IsNew = true,
                    Duration = model.Duration,
                    Format = model.Format,
                    EmployerId = employer?.Id
                };

                await _supabase.From<Course>().Insert(course);
                TempData["SuccessMessage"] = "Thêm khóa học thành công.";
                return RedirectToAction("MyCourses");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> EditCourse(int id)
        {
            try
            {
                var employer = await GetCurrentEmployerAsync();
                if (employer == null) return RedirectToAction("Index");

                var course = await _supabase.From<Course>().Where(c => c.Id == id && c.EmployerId == employer.Id).Single();
                if (course == null) return NotFound();

                var viewModel = new JobNow.ViewModels.CreateCourseViewModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    ProviderName = course.ProviderName,
                    ProviderLogo = course.ProviderLogo,
                    Price = course.Price,
                    Location = course.Location,
                    Deadline = course.Deadline,
                    Tags = course.Tags,
                    Duration = course.Duration,
                    Format = course.Format
                };

                return View(viewModel);
            }
            catch
            {
                return RedirectToAction("MyCourses");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, JobNow.ViewModels.CreateCourseViewModel model)
        {
            if (id != model.Id || !ModelState.IsValid) return View(model);

            try
            {
                var employer = await GetCurrentEmployerAsync();
                if (employer == null) return Unauthorized();

                var course = await _supabase.From<Course>().Where(c => c.Id == id && c.EmployerId == employer.Id).Single();
                if (course == null) return NotFound();

                course.Title = model.Title;
                course.ProviderName = model.ProviderName;
                course.ProviderLogo = model.ProviderLogo;
                course.Price = model.Price;
                course.Location = model.Location;
                course.Deadline = model.Deadline;
                course.Tags = model.Tags;
                course.Duration = model.Duration;
                course.Format = model.Format;

                await _supabase.From<Course>().Update(course);
                TempData["SuccessMessage"] = "Cập nhật khóa học thành công.";
                return RedirectToAction("MyCourses");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var employer = await GetCurrentEmployerAsync();
                if (employer != null)
                {
                    await _supabase.From<Course>().Where(c => c.Id == id && c.EmployerId == employer.Id).Delete();
                    TempData["SuccessMessage"] = "Xóa khóa học thành công.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("MyCourses");
        }

        private async Task<Employer?> GetCurrentEmployerAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            try
            {
                var employer = await _supabase.From<Employer>()
                    .Where(e => e.ProfileId == userId)
                    .Single();

                if (employer == null)
                {
                    // Fetch profile to get name and email
                    var profile = await _supabase.From<Profile>().Where(p => p.Id == userId).Single();
                    
                    var newEmployer = new Employer
                    {
                        Name = !string.IsNullOrWhiteSpace(profile?.FullName) ? profile.FullName : "New Company",
                        Email = profile?.Email ?? "",
                        ProfileId = userId,
                        IsGlobal = false
                    };
                    
                    var insertResponse = await _supabase.From<Employer>().Insert(newEmployer);
                    employer = insertResponse.Models.FirstOrDefault() ?? newEmployer;
                }

                return employer;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IActionResult> CompanyProfile()
        {
            try
            {
                var employer = await GetCurrentEmployerAsync();

                if (employer == null) return RedirectToAction("Index");

                var model = new CompanyProfileViewModel
                {
                    Id = employer.Id,
                    Name = employer.Name,
                    LogoUrl = employer.LogoUrl,
                    Website = employer.Website,
                    Email = employer.Email,
                    Phone = employer.Phone,
                    Address = employer.Address,
                    CompanySize = employer.CompanySize,
                    Description = employer.Description
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi tải thông tin: " + ex.Message;
                return View(new CompanyProfileViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyProfilePost(CompanyProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("CompanyProfile", model);
            }

            try
            {
                var employer = await GetCurrentEmployerAsync();

                if (employer != null)
                {
                    employer.Name = model.Name;
                    employer.LogoUrl = model.LogoUrl;
                    employer.Website = model.Website;
                    employer.Email = model.Email;
                    employer.Phone = model.Phone;
                    employer.Address = model.Address;
                    employer.CompanySize = model.CompanySize;
                    employer.Description = model.Description;

                    await _supabase.From<Employer>().Update(employer);
                    TempData["SuccessMessage"] = "Cập nhật hồ sơ công ty thành công!";
                }

                return RedirectToAction("CompanyProfile");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi lưu thông tin: " + ex.Message;
                return View("CompanyProfile", model);
            }
        }

        public async Task<IActionResult> Applicants(int jobId)
        {
            var employer = await GetCurrentEmployerAsync();
            if (employer == null) return RedirectToAction("Index");

            try
            {
                // Verify job belongs to employer
                var job = await _supabase.From<Job>().Where(j => j.Id == jobId && j.EmployerId == employer.Id).Single();
                if (job == null) return NotFound("Không tìm thấy tin tuyển dụng hoặc bạn không có quyền truy cập.");

                ViewBag.JobTitle = job.Title;
                ViewBag.JobId = job.Id;

                var appsResponse = await _supabase.From<Application>().Filter("job_id", Postgrest.Constants.Operator.Equals, jobId).Get();
                var applications = appsResponse.Models ?? new List<Application>();

                var profileIds = applications.Select(a => a.ProfileId).Distinct().ToList();
                var cvIds = applications.Where(a => a.CvId.HasValue).Select(a => a.CvId.Value).Distinct().ToList();

                if (profileIds.Any())
                {
                    var profilesResponse = await _supabase.From<Profile>().Filter("id", Postgrest.Constants.Operator.In, profileIds).Get();
                    if (profilesResponse.Models != null) 
                    {
                        var profilesDict = profilesResponse.Models.ToDictionary(p => p.Id);
                        foreach (var app in applications)
                        {
                            if (profilesDict.TryGetValue(app.ProfileId, out var profile)) app.Profile = profile;
                        }
                    }
                }

                if (cvIds.Any())
                {
                    var cvsResponse = await _supabase.From<UserCV>().Filter("id", Postgrest.Constants.Operator.In, cvIds).Get();
                    if (cvsResponse.Models != null)
                    {
                        var cvsDict = cvsResponse.Models.ToDictionary(c => c.Id);
                        foreach (var app in applications)
                        {
                            if (app.CvId.HasValue && cvsDict.TryGetValue(app.CvId.Value, out var cv)) app.CV = cv;
                        }
                    }
                }

                return View(applications);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("MyJobs");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApplicationStatus(int appId, string status, string? reason)
        {
            var employer = await GetCurrentEmployerAsync();
            if (employer == null) return Unauthorized();

            if (status == "Rejected" && string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Cần có lý do khi từ chối ứng viên.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            try
            {
                var appResponse = await _supabase.From<Application>().Filter("id", Postgrest.Constants.Operator.Equals, appId).Get();
                var app = appResponse.Models?.FirstOrDefault();
                if (app == null) return NotFound("Application not found.");

                // Validate that the employer actually owns the job for this application
                var job = await _supabase.From<Job>().Where(j => j.Id == app.JobId && j.EmployerId == employer.Id).Single();
                if (job == null) return Unauthorized();

                app.Status = status;
                app.RejectionReason = status == "Rejected" ? reason : null;
                app.UpdatedAt = DateTime.UtcNow;

                await _supabase.From<Application>().Update(app);

                // Create Notification for Candidate
                if (status == "Accepted" || status == "Rejected")
                {
                    string actionMsg = status == "Accepted" ? "chấp nhận" : "từ chối";
                    string extraMsg = status == "Rejected" ? $" Lý do: {reason}" : " Chúc mừng bạn đã vượt qua vòng hồ sơ.";
                    
                    var notif = new Notification
                    {
                        ProfileId = app.ProfileId,
                        Title = $"Hồ sơ {actionMsg}",
                        Message = $"Nhà tuyển dụng {job.CompanyName} đã {actionMsg} hồ sơ ứng tuyển của bạn cho vị trí {job.Title}.{extraMsg}",
                        Type = "Application",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.ToString("O"),
                        ActionLink = "/Profile"
                    };
                    await _supabase.From<Notification>().Insert(notif);
                }

                TempData["SuccessMessage"] = "Cập nhật trạng thái thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }

        public IActionResult Settings()
        {
            return View();
        }
    }
}
