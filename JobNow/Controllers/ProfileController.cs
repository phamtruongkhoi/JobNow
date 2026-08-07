using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using JobNow.Models;
using JobNow.ViewModels;
using JobNow.Services;

namespace JobNow.Controllers
{
    /// <summary>
    /// ProfileController: Xử lý hiển thị Tab "Hồ sơ của tôi", "Quản lý CV", cập nhật thông tin cá nhân,
    /// tải lên ảnh đại diện (Avatar) và tệp CV (PDF/Word/Ảnh) vào Supabase Storage, quản lý lịch sử làm việc.
    /// Thực hiện theo nguyên tắc Clean Architecture với chú thích tiếng Việt rõ ràng.
    /// </summary>
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly Supabase.Client _supabase;

        public ProfileController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        // =========================================================================
        // 1. ACTION HIỂN THỊ TRANG CÁ NHÂN (TAB HỒ SƠ CỦA TÔI & QUẢN LÝ CV)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy User ID của ứng viên đang đăng nhập từ Claim
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // 1.1. Truy vấn dữ liệu Profile từ bảng jn_profiles
                Profile? profile = null;
                try
                {
                    profile = await _supabase.From<Profile>()
                        .Where(p => p.Id == userId)
                        .Single();
                }
                catch
                {
                    profile = null;
                }

                // Nếu chưa có profile trong bảng, khởi tạo đối tượng mặc định với ID và Email từ Authentication
                if (profile == null)
                {
                    profile = new Profile
                    {
                        Id = userId,
                        Email = User.FindFirstValue(ClaimTypes.Email),
                        FullName = User.Identity?.Name
                    };
                }

                // 1.2. Truy vấn danh sách lịch sử làm việc từ bảng jn_work_histories
                List<WorkHistory> workHistories = new List<WorkHistory>();
                try
                {
                    var workRes = await _supabase.From<WorkHistory>()
                        .Where(w => w.ProfileId == userId)
                        .Order("start_date", Postgrest.Constants.Ordering.Descending)
                        .Get();

                    if (workRes.Models != null)
                    {
                        workHistories = workRes.Models;
                    }
                }
                catch
                {
                    workHistories = new List<WorkHistory>();
                }

                // 1.3. Truy vấn danh sách CV từ bảng jn_user_cvs
                List<UserCV> userCVs = new List<UserCV>();
                try
                {
                    var cvRes = await _supabase.From<UserCV>()
                        .Where(c => c.ProfileId == userId)
                        .Order("upload_date", Postgrest.Constants.Ordering.Descending)
                        .Get();

                    if (cvRes.Models != null)
                    {
                        userCVs = cvRes.Models;
                    }
                }
                catch
                {
                    userCVs = new List<UserCV>();
                }

                // 1.4. Truy vấn danh sách việc làm đã lưu từ bảng jn_saved_jobs và JOIN với bảng jn_jobs
                List<SavedJob> savedJobs = new List<SavedJob>();
                try
                {
                    var savedRes = await _supabase.From<SavedJob>()
                        .Where(s => s.ProfileId == userId)
                        .Order("saved_at", Postgrest.Constants.Ordering.Descending)
                        .Get();

                    if (savedRes.Models != null && savedRes.Models.Count > 0)
                    {
                        savedJobs = savedRes.Models;

                        // Truy vấn thông tin Job (JOIN trong bộ nhớ đảm bảo 100% chính xác, tránh N+1 query)
                        var jobIds = savedJobs.Select(s => s.JobId).Distinct().ToList();
                        var jobsRes = await _supabase.From<Job>()
                            .Filter("id", Postgrest.Constants.Operator.In, jobIds)
                            .Get();

                        var jobsMap = new Dictionary<int, Job>();
                        if (jobsRes.Models != null)
                        {
                            foreach (var j in jobsRes.Models)
                            {
                                jobsMap[j.Id] = j;
                            }
                        }

                        foreach (var sj in savedJobs)
                        {
                            if (sj.Job == null && jobsMap.TryGetValue(sj.JobId, out var jobObj))
                            {
                                sj.Job = jobObj;
                            }
                        }
                    }
                }
                catch
                {
                    savedJobs = new List<SavedJob>();
                }

                // 1.5. Truy vấn danh sách việc làm đã ứng tuyển (Applications)
                List<Application> applications = new List<Application>();
                try
                {
                    var appRes = await _supabase.From<Application>()
                        .Where(a => a.ProfileId == userId)
                        .Order("applied_at", Postgrest.Constants.Ordering.Descending)
                        .Get();

                    if (appRes.Models != null && appRes.Models.Count > 0)
                    {
                        applications = appRes.Models;

                        // Truy vấn thông tin Job
                        var appJobIds = applications.Select(a => a.JobId).Distinct().ToList();
                        var appJobsRes = await _supabase.From<Job>()
                            .Filter("id", Postgrest.Constants.Operator.In, appJobIds)
                            .Get();

                        var appJobsMap = new Dictionary<int, Job>();
                        if (appJobsRes.Models != null)
                        {
                            foreach (var j in appJobsRes.Models)
                            {
                                appJobsMap[j.Id] = j;
                            }
                        }

                        foreach (var app in applications)
                        {
                            if (app.Job == null && appJobsMap.TryGetValue(app.JobId, out var jobObj))
                            {
                                app.Job = jobObj;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR IN PROFILE APPLICATIONS: " + ex.ToString());
                    applications = new List<Application>();
                }

                // 1.6. Đóng gói dữ liệu vào ViewModel để gửi sang Razor View
                var viewModel = new ProfileViewModel
                {
                    Profile = profile,
                    WorkHistories = workHistories,
                    CVs = userCVs,
                    SavedJobs = savedJobs,
                    Applications = applications
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi tải trang cá nhân: {ex.Message}";
                return View(new ProfileViewModel());
            }
        }

        // =========================================================================
        // 1.5. ACTION DANH SÁCH HỒ SƠ ỨNG TUYỂN
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> MyApplications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

            var applications = new List<Application>();
            try
            {
                var appsResponse = await _supabase.From<Application>()
                    .Where(a => a.ProfileId == userId)
                    .Order("applied_at", Postgrest.Constants.Ordering.Descending)
                    .Get();

                if (appsResponse.Models != null && appsResponse.Models.Any())
                {
                    applications = appsResponse.Models;
                    
                    // Lấy Job và Employer để hiển thị
                    var jobIds = applications.Select(a => a.JobId).Distinct().ToList();
                    var jobsRes = await _supabase.From<Job>().Filter("id", Postgrest.Constants.Operator.In, jobIds).Get();
                    
                    if (jobsRes.Models != null)
                    {
                        var jobsDict = jobsRes.Models.ToDictionary(j => j.Id);
                        var employerIds = jobsRes.Models.Where(j => j.EmployerId.HasValue).Select(j => j.EmployerId.Value).Distinct().ToList();
                        
                        var empRes = await _supabase.From<Employer>().Filter("id", Postgrest.Constants.Operator.In, employerIds).Get();
                        var empDict = empRes.Models?.ToDictionary(e => e.Id) ?? new Dictionary<int, Employer>();

                        foreach (var app in applications)
                        {
                            if (jobsDict.TryGetValue(app.JobId, out var job))
                            {
                                if (job.EmployerId.HasValue && empDict.TryGetValue(job.EmployerId.Value, out var emp)) job.Employer = emp;
                                app.Job = job;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
            }

            return View(applications);
        }

        // =========================================================================
        // 2. ACTION CẬP NHẬT THÔNG TIN CÁ NHÂN
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] Profile model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                model.Id = userId;
                model.UpdatedAt = DateTime.UtcNow;

                await _supabase.From<Profile>().Upsert(model);

                TempData["Success"] = "Cập nhật hồ sơ cá nhân thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Không thể cập nhật hồ sơ: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 3. ACTION UPLOAD ẢNH ĐẠI DIỆN (AVATAR) LÊN SUPABASE STORAGE
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatarFile)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

                if (avatarFile == null || avatarFile.Length == 0)
                {
                    TempData["Error"] = "Vui lòng chọn ảnh trước khi tải lên!";
                    return RedirectToAction(nameof(Index));
                }

                string bucketName = "jn_avatars";

                var ext = Path.GetExtension(avatarFile.FileName ?? "avatar.jpg").ToLower();
                var fileName = $"{userId}/avatar_{DateTime.UtcNow.Ticks}{ext}";

                using var stream = new MemoryStream();
                await avatarFile.CopyToAsync(stream);
                var fileBytes = stream.ToArray();

                var fileOptions = new Supabase.Storage.FileOptions
                {
                    Upsert = true,
                    ContentType = avatarFile.ContentType ?? "image/jpeg"
                };

                try
                {
                    await _supabase.Storage.From(bucketName).Upload(fileBytes, fileName, fileOptions);
                }
                catch (Exception storageEx)
                {
                    TempData["Error"] = $"Lỗi Supabase Storage (Ảnh): {storageEx.Message}";
                    return RedirectToAction(nameof(Index));
                }

                var avatarUrl = _supabase.Storage.From(bucketName).GetPublicUrl(fileName);

                Profile? profile = null;
                try { profile = await _supabase.From<Profile>().Where(p => p.Id == userId).Single(); }
                catch { profile = new Profile { Id = userId }; }

                profile.AvatarUrl = avatarUrl;
                profile.UpdatedAt = DateTime.UtcNow;
                await _supabase.From<Profile>().Upsert(profile);

                TempData["Success"] = "Cập nhật ảnh đại diện thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi hệ thống khi tải ảnh: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 4. ACTION UPLOAD FILE CV (PDF/WORD/ẢNH) LÊN SUPABASE STORAGE
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCV(IFormFile? cvFile, bool isDefault = false)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

                if (cvFile == null || cvFile.Length == 0)
                {
                    TempData["Error"] = "Vui lòng chọn file CV cần tải lên!";
                    return RedirectToAction(nameof(Index));
                }

                var ext = Path.GetExtension(cvFile.FileName ?? "file.pdf").ToLower();
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                if (Array.IndexOf(allowedExtensions, ext) < 0 || cvFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "Sai định dạng (.pdf, .doc, .docx, .jpg, .png) hoặc file vượt quá 10MB!";
                    return RedirectToAction(nameof(Index));
                }

                var existingRes = await _supabase.From<UserCV>().Where(c => c.ProfileId == userId).Get();
                var existingCVs = existingRes.Models ?? new List<UserCV>();

                if (existingCVs.Count == 0) isDefault = true;
                else if (isDefault)
                {
                    foreach (var item in existingCVs.Where(i => i.IsDefault))
                    {
                        item.IsDefault = false;
                        await _supabase.From<UserCV>().Update(item);
                    }
                }

                string bucketName = "jn_cv_uploads";
                var storageFileName = $"{userId}/cv_{DateTime.UtcNow.Ticks}{ext}";

                using var stream = new MemoryStream();
                await cvFile.CopyToAsync(stream);
                var fileBytes = stream.ToArray();

                var fileOptions = new Supabase.Storage.FileOptions
                {
                    Upsert = true,
                    ContentType = cvFile.ContentType ?? "application/pdf"
                };

                try
                {
                    await _supabase.Storage.From(bucketName).Upload(fileBytes, storageFileName, fileOptions);
                }
                catch (Exception storageEx)
                {
                    TempData["Error"] = $"Lỗi Supabase Storage (CV): {storageEx.Message}";
                    return RedirectToAction(nameof(Index));
                }

                var fileUrl = _supabase.Storage.From(bucketName).GetPublicUrl(storageFileName);

                var newCV = new UserCV
                {
                    ProfileId = userId,
                    FileName = cvFile.FileName,
                    FileUrl = fileUrl,
                    Size = cvFile.Length,
                    UploadDate = DateTime.UtcNow,
                    IsDefault = isDefault
                };

                await _supabase.From<UserCV>().Insert(newCV);
                TempData["Success"] = "Tải lên hồ sơ CV thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi hệ thống khi tải file CV: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 5. ACTION ĐẶT CV LÀM MẶC ĐỊNH (UPDATE IsDefault)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultCV(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var cvRes = await _supabase.From<UserCV>().Where(c => c.ProfileId == userId).Get();
                if (cvRes.Models != null)
                {
                    foreach (var item in cvRes.Models)
                    {
                        if (item.Id == id && !item.IsDefault)
                        {
                            item.IsDefault = true;
                            await _supabase.From<UserCV>().Update(item);
                        }
                        else if (item.Id != id && item.IsDefault)
                        {
                            item.IsDefault = false;
                            await _supabase.From<UserCV>().Update(item);
                        }
                    }
                }

                TempData["Success"] = "Đã chọn CV làm hồ sơ mặc định!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Không thể đặt CV mặc định: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 6. ACTION XÓA CV (XÓA CẢ TRÊN STORAGE VÀ DATABASE)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCV(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var cv = await _supabase.From<UserCV>()
                    .Where(c => c.Id == id && c.ProfileId == userId)
                    .Single();

                if (cv != null)
                {
                    if (!string.IsNullOrEmpty(cv.FileUrl) && cv.FileUrl.Contains("/jn_cv_uploads/"))
                    {
                        try
                        {
                            var storagePath = cv.FileUrl.Substring(cv.FileUrl.IndexOf("/jn_cv_uploads/") + "/jn_cv_uploads/".Length);
                            await _supabase.Storage.From("jn_cv_uploads").Remove(new List<string> { storagePath });
                        }
                        catch
                        {
                            // Vẫn tiếp tục xóa dưới DB ngay cả khi file trên storage bị thiếu/lỗi path
                        }
                    }

                    await _supabase.From<UserCV>()
                        .Where(c => c.Id == id && c.ProfileId == userId)
                        .Delete();

                    if (cv.IsDefault)
                    {
                        var remainingRes = await _supabase.From<UserCV>()
                            .Where(c => c.ProfileId == userId)
                            .Order("upload_date", Postgrest.Constants.Ordering.Descending)
                            .Get();

                        if (remainingRes.Models != null && remainingRes.Models.Count > 0)
                        {
                            var latestCV = remainingRes.Models[0];
                            latestCV.IsDefault = true;
                            await _supabase.From<UserCV>().Update(latestCV);
                        }
                    }
                }

                TempData["Success"] = "Đã xóa CV khỏi hệ thống và bộ nhớ!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa CV: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 7. ACTION TẢI XUỐNG CV (DOWNLOAD CV)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> DownloadCV(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var cv = await _supabase.From<UserCV>()
                    .Where(c => c.Id == id && c.ProfileId == userId)
                    .Single();

                if (cv == null || string.IsNullOrEmpty(cv.FileUrl))
                {
                    TempData["Error"] = "Không tìm thấy file CV yêu cầu!";
                    return RedirectToAction(nameof(Index));
                }

                using var httpClient = new HttpClient();
                var fileBytes = await httpClient.GetByteArrayAsync(cv.FileUrl);

                var ext = Path.GetExtension(cv.FileName)?.ToLower() ?? ".pdf";
                var contentType = ext switch
                {
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    _ => "application/pdf"
                };

                return File(fileBytes, contentType, cv.FileName ?? "CV_JobNow" + ext);
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
        }

        // =========================================================================
        // 8. ACTION THÊM KINH NGHIỆM LÀM VIỆC (WORK HISTORY)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWorkHistory(WorkHistory model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                model.ProfileId = userId;
                model.CreatedAt = DateTime.UtcNow;
                await _supabase.From<WorkHistory>().Insert(model);

                TempData["Success"] = "Đã bổ sung kinh nghiệm làm việc vào hồ sơ!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Không thể thêm kinh nghiệm làm việc: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 9. ACTION XÓA KINH NGHIỆM LÀM VIỆC
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWorkHistory(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _supabase.From<WorkHistory>()
                    .Where(w => w.Id == id && w.ProfileId == userId)
                    .Delete();

                TempData["Success"] = "Đã xóa kinh nghiệm làm việc khỏi hồ sơ.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa kinh nghiệm làm việc: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 10. ACTION PHÂN TÍCH VÀ TƯ VẤN CV BẰNG AI (GOOGLE GEMINI)
        // =========================================================================
        [HttpPost]
        [IgnoreAntiforgeryToken] // Hỗ trợ AJAX gọi trực tiếp mượt mà từ nút bấm AI
        public async Task<IActionResult> AnalyzeCVWithAI(int id, [FromServices] GeminiService geminiService)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để sử dụng tính năng tư vấn AI." });
                }

                // 10.1. Tìm thông tin CV trong cơ sở dữ liệu jn_user_cvs
                var cv = await _supabase.From<UserCV>()
                    .Where(c => c.Id == id && c.ProfileId == userId)
                    .Single();

                if (cv == null || string.IsNullOrEmpty(cv.FileUrl))
                {
                    return Json(new { success = false, message = "Không tìm thấy hồ sơ CV hoặc file bị thiếu URL." });
                }

                // 10.2. Tải mảng byte[] của file CV từ URL Supabase Storage
                using var httpClient = new HttpClient();
                var fileBytes = await httpClient.GetByteArrayAsync(cv.FileUrl);

                string aiAdvice = "";
                var ext = Path.GetExtension(cv.FileName)?.ToLower() ?? ".pdf";

                // XỬ LÝ 1: NẾU LÀ FILE ẢNH -> GỌI GEMINI VISION ĐỂ ĐỌC ẢNH
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                {
                    string mimeType = ext == ".png" ? "image/png" : "image/jpeg";
                    aiAdvice = await geminiService.AnalyzeImageCVAsync(fileBytes, mimeType);
                }
                // XỬ LÝ 2: NẾU LÀ PDF -> ĐỌC TEXT BẰNG ITEXT NHƯ CŨ
                else if (ext == ".pdf")
                {
                    try
                    {
                        using var pdfReader = new PdfReader(new MemoryStream(fileBytes));
                        using var pdfDoc = new PdfDocument(pdfReader);
                        var sb = new StringBuilder();

                        int maxPages = Math.Min(pdfDoc.GetNumberOfPages(), 5); // Đọc tối đa 5 trang đầu
                        for (int i = 1; i <= maxPages; i++)
                        {
                            var page = pdfDoc.GetPage(i);
                            var strategy = new LocationTextExtractionStrategy();
                            var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                            sb.AppendLine(pageText);
                        }
                        aiAdvice = await geminiService.AnalyzeCVAsync(sb.ToString());
                    }
                    catch (Exception exPdf)
                    {
                        aiAdvice = $"Lỗi đọc PDF: {exPdf.Message}";
                    }
                }
                // XỬ LÝ 3: FILE WORD HOẶC ĐỊNH DẠNG KHÁC
                else
                {
                    aiAdvice = "Hệ thống AI hiện tại phân tích tốt nhất với định dạng **PDF** hoặc **Hình ảnh (JPG/PNG)**. Hãy xuất CV của bạn ra PDF hoặc chụp ảnh CV để AI có thể nhìn thấy chi tiết và tư vấn chính xác nhất nhé!";
                }

                return Json(new
                {
                    success = true,
                    cvName = cv.FileName,
                    advice = aiAdvice
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Lỗi hệ thống khi tư vấn AI: {ex.Message}"
                });
            }
        }

        // =========================================================================
        // 11. ACTION XÓA CÔNG VIỆC KHỎI DANH SÁCH ĐÃ LƯU (DELETE SAVED JOB)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSavedJob(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Xóa công việc đã lưu trong bảng jn_saved_jobs
                await _supabase.From<SavedJob>()
                    .Where(s => s.Id == id && s.ProfileId == userId)
                    .Delete();

                TempData["Success"] = "Đã xóa công việc khỏi danh sách lưu thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa công việc đã lưu: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}