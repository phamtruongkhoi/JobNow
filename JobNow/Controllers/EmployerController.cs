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
                    var emptyModel = new EmployerDashboardViewModel
                    {
                        TotalJobs = 0, ActiveJobs = 0, ExpiredJobs = 0, TotalApplicants = 0, NoEmployerProfile = true,
                        TokenBalance = 0, DailyFreeToken = 0, PostJobCost = 0, NextDailyClaim = ""
                    };
                    return View(emptyModel);
                }

                var response = await _supabase.From<Job>()
                    .Where(j => j.EmployerId == employer.Id)
                    .Get();
                var jobs = response.Models;

                var tokenResponse = await _supabase.From<EmployerToken>().Where(t => t.EmployerId == employer.Id).Get();
                var token = tokenResponse.Models?.FirstOrDefault();
                
                var settingsResponse = await _supabase.From<TokenSettings>().Get();
                var settings = settingsResponse.Models?.FirstOrDefault();

                var jobIds = jobs.Select(j => j.Id).ToList();
                var applications = new System.Collections.Generic.List<Application>();
                if (jobIds.Any())
                {
                    var appsResponse = await _supabase.From<Application>().Filter("job_id", Postgrest.Constants.Operator.In, jobIds).Get();
                    applications = appsResponse.Models ?? new System.Collections.Generic.List<Application>();
                }

                var model = new EmployerDashboardViewModel
                {
                    TotalJobs = jobs.Count,
                    ActiveJobs = jobs.Count(j => j.Status == "Published" && j.Deadline >= DateTime.Now),
                    ExpiredJobs = jobs.Count(j => j.Deadline < DateTime.Now),
                    TotalApplicants = jobs.Sum(j => j.AppliedCount),
                    NoEmployerProfile = false,
                    TokenBalance = token?.Balance ?? 0,
                    DailyFreeToken = settings?.DailyFreeToken ?? 0,
                    PostJobCost = settings?.PostJobCost ?? 0,
                    NextDailyClaim = token?.LastDailyClaim?.Date == DateTime.UtcNow.Date ? "Ngày mai" : "Sẵn sàng",
                    ApplicationsReceived = applications.Count,
                    ApplicationsReviewing = applications.Count(a => a.Status == "Reviewing"),
                    ApplicationsInterview = applications.Count(a => a.Status == "Interview"),
                    ApplicationsAccepted = applications.Count(a => a.Status == "Accepted"),
                    ApplicationsRejected = applications.Count(a => a.Status == "Rejected")
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(new EmployerDashboardViewModel());
            }
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
                var jobs = response.Models.OrderByDescending(j => j.CreatedAt).ToList();

                var applicationsResponse = await _supabase.From<Application>().Get();
                if (applicationsResponse.Models != null)
                {
                    var allApplications = applicationsResponse.Models;
                    foreach (var job in jobs)
                    {
                        job.AppliedCount = allApplications.Count(a => a.JobId == job.Id);
                    }
                }

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
                var employer = await GetCurrentEmployerAsync();
                if (employer == null) return Unauthorized();

                var settingsResponse = await _supabase.From<TokenSettings>().Get();
                var settings = settingsResponse.Models?.FirstOrDefault();
                var postJobCost = settings?.PostJobCost ?? 0;
                
                int durationCost = 0;
                if (model.DurationDays == 7) durationCost = 2;
                else if (model.DurationDays == 15) durationCost = 5;
                else if (model.DurationDays == 30) durationCost = 10;
                
                int badgeCost = 0;
                string badgeLabel = "";
                if (model.BadgeType == "verified") { badgeCost = 5; badgeLabel = "Công ty uy tín"; }
                else if (model.BadgeType == "top") { badgeCost = 8; badgeLabel = "Top Employer"; }
                else if (model.BadgeType == "urgent") { badgeCost = 3; badgeLabel = "Tuyển gấp"; }
                
                int totalCost = postJobCost + durationCost + badgeCost;

                var tokenResponse = await _supabase.From<EmployerToken>().Where(t => t.EmployerId == employer.Id).Get();
                var token = tokenResponse.Models?.FirstOrDefault();
                var currentBalance = token?.Balance ?? 0;

                if (currentBalance < totalCost)
                {
                    ModelState.AddModelError("", "Bạn không đủ Token để đăng tin.");
                    return View("CreateJob", model);
                }

                // Deduct token
                if (token != null)
                {
                    token.Balance -= totalCost;
                    token.UpdatedAt = DateTime.UtcNow;
                    
                    var response = await _supabase.From<EmployerToken>()
                        .Where(t => t.EmployerId == employer.Id)
                        .Set(t => t.Balance, token.Balance)
                        .Set(t => t.UpdatedAt, token.UpdatedAt)
                        .Update();
                        
                    if (response.Models == null || !response.Models.Any())
                    {
                        await _supabase.From<EmployerToken>().Update(token);
                    }
                }
                else
                {
                    token = new EmployerToken { EmployerId = employer.Id, Balance = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    await _supabase.From<EmployerToken>().Insert(token);
                }

                // Log transaction
                if (totalCost > 0)
                {
                    string desc = $"Đăng tin tuyển dụng ({model.DurationDays} ngày";
                    if (!string.IsNullOrEmpty(badgeLabel)) desc += $" + {badgeLabel}";
                    desc += ")";
                    
                    var transaction = new TokenTransaction
                    {
                        EmployerId = employer.Id,
                        TransactionType = "Consume",
                        TokenAmount = totalCost,
                        Status = "Completed",
                        Description = desc,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _supabase.From<TokenTransaction>().Insert(transaction);

                    if (!string.IsNullOrEmpty(employer.ProfileId))
                    {
                        var notification = new Notification
                        {
                            ProfileId = employer.ProfileId,
                            Title = "Sử dụng Token",
                            Message = $"Bạn đã sử dụng {totalCost} Token để {desc.ToLower()}.",
                            Type = "System",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.ToString("O"),
                            ActionLink = "/Employer/Wallet"
                        };
                        await _supabase.From<Notification>().Insert(notification);
                    }
                }

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
                    Status = "Pending",
                    PostedAt = DateTime.Now.ToString("dd/MM/yyyy"), // Giữ format cũ cho tương thích
                    CreatedAt = DateTime.UtcNow,
                    ExpiredAt = DateTime.UtcNow.AddDays(model.DurationDays),
                    DurationDays = model.DurationDays,
                    BadgeType = model.BadgeType,
                    UpdatedAt = DateTime.UtcNow,
                    AppliedCount = 0,
                    IsHot = model.BadgeType == "top",
                    IsUrgent = model.BadgeType == "urgent",
                    IsNew = true,
                    EmployerId = employer.Id
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
                existingJob.Status = "Pending";
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
            if (!string.IsNullOrEmpty(model.RegisterUrl) && !model.RegisterUrl.StartsWith("http://") && !model.RegisterUrl.StartsWith("https://"))
            {
                ModelState.AddModelError("RegisterUrl", "Chỉ chấp nhận link HTTP hoặc HTTPS");
            }

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
                    RegisterUrl = model.RegisterUrl,
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
                    Format = course.Format,
                    RegisterUrl = course.RegisterUrl
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
            if (!string.IsNullOrEmpty(model.RegisterUrl) && !model.RegisterUrl.StartsWith("http://") && !model.RegisterUrl.StartsWith("https://"))
            {
                ModelState.AddModelError("RegisterUrl", "Chỉ chấp nhận link HTTP hoặc HTTPS");
            }

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
                course.RegisterUrl = model.RegisterUrl;

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
        public async Task<IActionResult> UpdateApplicationStatus(int appId, string status)
        {
            var employer = await GetCurrentEmployerAsync();
            if (employer == null) return Unauthorized();

            try
            {
                var appResponse = await _supabase.From<Application>().Filter("id", Postgrest.Constants.Operator.Equals, appId).Get();
                var app = appResponse.Models?.FirstOrDefault();
                if (app == null) return NotFound("Application not found.");

                if (app.Status == "Accepted" || app.Status == "Rejected")
                {
                    TempData["ErrorMessage"] = "Không thể thay đổi trạng thái khi quy trình đã hoàn tất.";
                    return Redirect(Request.Headers["Referer"].ToString());
                }

                // Strict validation flow (Applied -> Reviewing -> Interview -> Accepted | Rejected)
                bool isValid = false;
                
                // Treat "Pending" as "Applied" for backward compatibility
                string currentStatus = app.Status == "Pending" ? "Applied" : app.Status;

                if (currentStatus == status) 
                {
                    TempData["SuccessMessage"] = "Đã cập nhật trạng thái hồ sơ.";
                    return Redirect(Request.Headers["Referer"].ToString());
                }

                if (currentStatus == "Applied" && (status == "Reviewing" || status == "Rejected")) isValid = true;
                if (currentStatus == "Reviewing" && (status == "Interview" || status == "Rejected")) isValid = true;
                if (currentStatus == "Interview" && (status == "Accepted" || status == "Rejected")) isValid = true;

                if (!isValid)
                {
                    TempData["ErrorMessage"] = "Chuyển đổi trạng thái không hợp lệ.";
                    return Redirect(Request.Headers["Referer"].ToString());
                }

                var job = await _supabase.From<Job>().Where(j => j.Id == app.JobId && j.EmployerId == employer.Id).Single();
                if (job == null) return Unauthorized();

                app.Status = status;
                app.UpdatedAt = DateTime.UtcNow;

                await _supabase.From<Application>().Update(app);

                // Notifications
                if (status != "Applied")
                {
                    string notifMsg = "";
                    if (status == "Reviewing") notifMsg = $"Hồ sơ ứng tuyển cho vị trí '{job.Title}' đang được xem xét.";
                    else if (status == "Interview") notifMsg = $"Bạn đã được mời phỏng vấn cho vị trí '{job.Title}'.";
                    else if (status == "Accepted") notifMsg = $"Chúc mừng! Bạn đã được nhận vào vị trí '{job.Title}'.";
                    else if (status == "Rejected") notifMsg = $"Rất tiếc, hồ sơ ứng tuyển cho vị trí '{job.Title}' chưa phù hợp.";

                    var notif = new Notification
                    {
                        ProfileId = app.ProfileId,
                        Title = $"Cập nhật trạng thái ứng tuyển",
                        Message = notifMsg,
                        Type = "Application",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.ToString("O"),
                        ActionLink = "/Profile/MyApplications"
                    };
                    await _supabase.From<Notification>().Insert(notif);
                }

                TempData["SuccessMessage"] = "Đã cập nhật trạng thái hồ sơ.";
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

        public async Task<IActionResult> Wallet()
        {
            var employer = await GetCurrentEmployerAsync();
            if (employer == null) return RedirectToAction("Index");

            var tokenResponse = await _supabase.From<EmployerToken>().Where(t => t.EmployerId == employer.Id).Get();
            var token = tokenResponse.Models?.FirstOrDefault();

            var settingsResponse = await _supabase.From<TokenSettings>().Get();
            var settings = settingsResponse.Models?.FirstOrDefault();

            var transactionsResponse = await _supabase.From<TokenTransaction>().Where(t => t.EmployerId == employer.Id).Get();
            var transactions = transactionsResponse.Models?.OrderByDescending(t => t.CreatedAt).ToList() ?? new List<TokenTransaction>();

            var packagesResponse = await _supabase.From<TokenPackage>().Where(p => p.IsActive == true).Get();
            var packages = packagesResponse.Models ?? new List<TokenPackage>();

            bool canClaimDaily = token == null || !token.LastDailyClaim.HasValue || token.LastDailyClaim.Value.Date < DateTime.UtcNow.Date;

            var model = new EmployerWalletViewModel
            {
                TokenBalance = token?.Balance ?? 0,
                DailyFreeToken = settings?.DailyFreeToken ?? 0,
                NextDailyClaim = canClaimDaily ? "Sẵn sàng" : "Ngày mai",
                CanClaimDaily = canClaimDaily,
                Transactions = transactions,
                Packages = packages
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimDailyToken()
        {
            try
            {
                var employer = await GetCurrentEmployerAsync();
                if (employer == null) return Unauthorized();

                var tokenResponse = await _supabase.From<EmployerToken>().Where(t => t.EmployerId == employer.Id).Get();
                var token = tokenResponse.Models?.FirstOrDefault();

                var settingsResponse = await _supabase.From<TokenSettings>().Get();
                var settings = settingsResponse.Models?.FirstOrDefault();
                var dailyFreeToken = settings?.DailyFreeToken ?? 0;

                if (token != null && token.LastDailyClaim.HasValue && token.LastDailyClaim.Value.Date >= DateTime.UtcNow.Date)
                {
                    TempData["ErrorMessage"] = "Bạn đã nhận Token miễn phí hôm nay.";
                    return RedirectToAction("Wallet");
                }

                bool updateSuccess = false;

                if (token != null)
                {
                    token.Balance += dailyFreeToken;
                    token.LastDailyClaim = DateTime.UtcNow;
                    token.UpdatedAt = DateTime.UtcNow;
                    
                    var response = await _supabase.From<EmployerToken>().Update(token);
                    if (response.Models != null && response.Models.Any())
                    {
                        updateSuccess = true;
                    }
                    else 
                    {
                        // Fallback in case Update by Id fails (e.g. Id is 0 or primary key mapping issue)
                        var fallbackResponse = await _supabase.From<EmployerToken>()
                            .Where(t => t.EmployerId == employer.Id)
                            .Set(t => t.Balance, token.Balance)
                            .Set(t => t.LastDailyClaim, token.LastDailyClaim)
                            .Set(t => t.UpdatedAt, token.UpdatedAt)
                            .Update();
                            
                        if (fallbackResponse.Models != null && fallbackResponse.Models.Any())
                        {
                            updateSuccess = true;
                        }
                    }
                }
                else
                {
                    token = new EmployerToken
                    {
                        EmployerId = employer.Id,
                        Balance = dailyFreeToken,
                        LastDailyClaim = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    var response = await _supabase.From<EmployerToken>().Insert(token);
                    if (response.Models != null && response.Models.Any())
                    {
                        updateSuccess = true;
                    }
                }

                if (updateSuccess)
                {
                    var transaction = new TokenTransaction
                    {
                        EmployerId = employer.Id,
                        TransactionType = "DailyReward",
                        TokenAmount = dailyFreeToken,
                        Status = "Completed",
                        Description = "Daily Free Token",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _supabase.From<TokenTransaction>().Insert(transaction);

                    if (!string.IsNullOrEmpty(employer.ProfileId))
                    {
                        var notification = new Notification
                        {
                            ProfileId = employer.ProfileId,
                            Title = "Nhận Token Miễn Phí",
                            Message = $"Bạn đã nhận {dailyFreeToken} Token miễn phí hôm nay.",
                            Type = "System",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.ToString("O"),
                            ActionLink = "/Employer/Wallet"
                        };
                        await _supabase.From<Notification>().Insert(notification);
                    }

                    TempData["SuccessMessage"] = $"Bạn đã nhận {dailyFreeToken} Token miễn phí hôm nay.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật ví Token. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Wallet");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyToken(int packageId)
        {
            var employer = await GetCurrentEmployerAsync();
            if (employer == null) return Unauthorized();

            var packageResponse = await _supabase.From<TokenPackage>().Where(p => p.Id == packageId).Get();
            var package = packageResponse.Models?.FirstOrDefault();

            if (package == null)
            {
                TempData["ErrorMessage"] = "Gói Token không hợp lệ.";
                return RedirectToAction("Wallet");
            }

            var transaction = new TokenTransaction
            {
                EmployerId = employer.Id,
                PackageId = packageId,
                TransactionType = "Purchase",
                TokenAmount = package.TokenAmount,
                MoneyAmount = package.Price,
                Status = "Pending",
                Description = $"Mua gói {package.PackageName}",
                CreatedAt = DateTime.UtcNow
            };

            await _supabase.From<TokenTransaction>().Insert(transaction);

            TempData["SuccessMessage"] = "Yêu cầu mua Token đã được gửi.";
            return RedirectToAction("Wallet");
        }
    }
}
