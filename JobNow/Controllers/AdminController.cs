using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using JobNow.Models;
using JobNow.ViewModels;
using System.Security.Claims;

namespace JobNow.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly Supabase.Client _supabase;

        public AdminController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        private async Task<bool> IsAdmin()
        {
            // TODO: Restore admin authorization check after the demo
            return true;
            
            /* Original Logic:
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return false;

            try
            {
                var profile = await _supabase.From<Profile>().Where(p => p.Id == userId).Single();
                return profile != null && profile.Role == "admin";
            }
            catch
            {
                return false;
            }
            */
        }

        public async Task<IActionResult> Index()
        {
            if (!await IsAdmin()) return Unauthorized("Bạn không có quyền truy cập trang quản trị.");

            try
            {
                var profilesRes = await _supabase.From<Profile>().Get();
                var employersRes = await _supabase.From<Employer>().Get();
                var jobsRes = await _supabase.From<Job>().Get();
                var coursesRes = await _supabase.From<Course>().Get();
                var appsRes = await _supabase.From<Application>().Get();

                var model = new AdminDashboardViewModel
                {
                    TotalUsers = profilesRes.Models?.Count ?? 0,
                    TotalEmployers = employersRes.Models?.Count ?? 0,
                    TotalJobs = jobsRes.Models?.Count ?? 0,
                    TotalCourses = coursesRes.Models?.Count ?? 0,
                    TotalApplications = appsRes.Models?.Count ?? 0
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(new AdminDashboardViewModel());
            }
        }

        public async Task<IActionResult> Users()
        {
            if (!await IsAdmin()) return Unauthorized();

            try
            {
                var response = await _supabase.From<Profile>().Get();
                return View(response.Models ?? new System.Collections.Generic.List<Profile>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Employers()
        {
            if (!await IsAdmin()) return Unauthorized();

            try
            {
                var response = await _supabase.From<Employer>().Get();
                return View(response.Models ?? new System.Collections.Generic.List<Employer>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Jobs()
        {
            if (!await IsAdmin()) return Unauthorized();

            try
            {
                var response = await _supabase.From<Job>().Get();
                return View(response.Models ?? new System.Collections.Generic.List<Job>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveJob(int id)
        {
            if (!await IsAdmin()) return Unauthorized();
            try
            {
                var job = await _supabase.From<Job>().Where(j => j.Id == id).Single();
                if (job != null)
                {
                    job.Status = "Published";
                    await _supabase.From<Job>().Update(job);

                    if (job.EmployerId.HasValue)
                    {
                        var employer = await _supabase.From<Employer>().Where(e => e.Id == job.EmployerId.Value).Single();
                        if (employer != null && !string.IsNullOrEmpty(employer.ProfileId))
                        {
                            var notification = new Notification
                            {
                                ProfileId = employer.ProfileId,
                                Title = "Tin tuyển dụng được duyệt",
                                Message = $"Tin tuyển dụng '{job.Title}' đã được quản trị viên phê duyệt.",
                                CreatedAt = DateTime.UtcNow.ToString("o"),
                                IsRead = false,
                                Type = "System",
                                ActionLink = "/Employer/MyJobs"
                            };
                            await _supabase.From<Notification>().Insert(notification);
                        }
                    }
                    TempData["SuccessMessage"] = "Đã phê duyệt tin tuyển dụng thành công.";
                }
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction("Jobs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectJob(int id)
        {
            if (!await IsAdmin()) return Unauthorized();
            try
            {
                var job = await _supabase.From<Job>().Where(j => j.Id == id).Single();
                if (job != null)
                {
                    job.Status = "Rejected";
                    await _supabase.From<Job>().Update(job);

                    if (job.EmployerId.HasValue)
                    {
                        var employer = await _supabase.From<Employer>().Where(e => e.Id == job.EmployerId.Value).Single();
                        if (employer != null && !string.IsNullOrEmpty(employer.ProfileId))
                        {
                            var notification = new Notification
                            {
                                ProfileId = employer.ProfileId,
                                Title = "Tin tuyển dụng bị từ chối",
                                Message = $"Tin tuyển dụng '{job.Title}' đã bị từ chối.",
                                CreatedAt = DateTime.UtcNow.ToString("o"),
                                IsRead = false,
                                Type = "System",
                                ActionLink = "/Employer/MyJobs"
                            };
                            await _supabase.From<Notification>().Insert(notification);
                        }
                    }
                    TempData["SuccessMessage"] = "Đã từ chối tin tuyển dụng.";
                }
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction("Jobs");
        }

        public async Task<IActionResult> Courses()
        {
            if (!await IsAdmin()) return Unauthorized();

            try
            {
                var response = await _supabase.From<Course>().Get();
                return View(response.Models ?? new System.Collections.Generic.List<Course>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int id)
        {
            if (!await IsAdmin()) return Unauthorized();
            try
            {
                await _supabase.From<Job>().Where(j => j.Id == id).Delete();
                TempData["SuccessMessage"] = "Xóa công việc thành công.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction("Jobs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            if (!await IsAdmin()) return Unauthorized();
            try
            {
                await _supabase.From<Course>().Where(c => c.Id == id).Delete();
                TempData["SuccessMessage"] = "Xóa khóa học thành công.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction("Courses");
        }

        public async Task<IActionResult> TokenTransactions(string filter = "Pending")
        {
            if (!await IsAdmin()) return Unauthorized();
            try
            {
                var transactionsResponse = await _supabase.From<TokenTransaction>().Where(t => t.TransactionType == "Purchase").Get();
                var transactions = transactionsResponse.Models ?? new System.Collections.Generic.List<TokenTransaction>();

                if (filter == "Pending" || filter == "Completed" || filter == "Rejected")
                {
                    transactions = transactions.Where(t => t.Status == filter).ToList();
                }

                transactions = transactions.OrderByDescending(t => t.CreatedAt).ToList();

                var employersResponse = await _supabase.From<Employer>().Get();
                var employers = employersResponse.Models?.ToDictionary(e => e.Id) ?? new System.Collections.Generic.Dictionary<int, Employer>();

                var packagesResponse = await _supabase.From<TokenPackage>().Get();
                var packages = packagesResponse.Models?.ToDictionary(p => p.Id) ?? new System.Collections.Generic.Dictionary<int, TokenPackage>();

                var items = new System.Collections.Generic.List<TokenTransactionItem>();
                foreach (var t in transactions)
                {
                    items.Add(new TokenTransactionItem
                    {
                        Transaction = t,
                        Employer = employers.ContainsKey(t.EmployerId) ? employers[t.EmployerId] : null,
                        Package = t.PackageId.HasValue && packages.ContainsKey(t.PackageId.Value) ? packages[t.PackageId.Value] : null
                    });
                }

                var model = new AdminTokenTransactionsViewModel
                {
                    Transactions = items,
                    Filter = filter
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTransaction(int id)
        {
            if (!await IsAdmin()) return Unauthorized();
            try
            {
                var transaction = await _supabase.From<TokenTransaction>().Where(t => t.Id == id).Single();
                if (transaction != null && transaction.Status == "Pending")
                {
                    transaction.Status = "Completed";
                    await _supabase.From<TokenTransaction>().Update(transaction);

                    var tokenResponse = await _supabase.From<EmployerToken>().Where(t => t.EmployerId == transaction.EmployerId).Get();
                    var token = tokenResponse.Models?.FirstOrDefault();

                    if (token != null)
                    {
                        token.Balance += transaction.TokenAmount;
                        token.UpdatedAt = DateTime.UtcNow;
                        
                        var response = await _supabase.From<EmployerToken>()
                            .Where(t => t.EmployerId == transaction.EmployerId)
                            .Set(t => t.Balance, token.Balance)
                            .Set(t => t.UpdatedAt, token.UpdatedAt)
                            .Update();
                            
                        if (response.Models == null || !response.Models.Any())
                        {
                            // Fallback if needed
                            await _supabase.From<EmployerToken>().Update(token);
                        }
                    }
                    else
                    {
                        token = new EmployerToken
                        {
                            EmployerId = transaction.EmployerId,
                            Balance = transaction.TokenAmount,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        // Note: We don't set LastDailyClaim here so it remains NULL (has not claimed yet)
                        await _supabase.From<EmployerToken>().Insert(token);
                    }

                    var employer = await _supabase.From<Employer>().Where(e => e.Id == transaction.EmployerId).Single();
                    var packageResponse = await _supabase.From<TokenPackage>().Where(p => p.Id == transaction.PackageId).Get();
                    var package = packageResponse.Models?.FirstOrDefault();
                    var packageName = package != null ? package.PackageName : "Token";

                    if (employer != null && !string.IsNullOrEmpty(employer.ProfileId))
                    {
                        var notification = new Notification
                        {
                            ProfileId = employer.ProfileId,
                            Title = "Phê duyệt mua Token",
                            Message = $"Gói {packageName} đã được phê duyệt (+{transaction.TokenAmount} Token).",
                            CreatedAt = DateTime.UtcNow.ToString("o"),
                            IsRead = false,
                            Type = "System",
                            ActionLink = "/Employer/Wallet"
                        };
                        await _supabase.From<Notification>().Insert(notification);
                    }

                    TempData["SuccessMessage"] = "Đã phê duyệt giao dịch thành công.";
                }
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction("TokenTransactions");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectTransaction(int id)
        {
            if (!await IsAdmin()) return Unauthorized();
            try
            {
                var transaction = await _supabase.From<TokenTransaction>().Where(t => t.Id == id).Single();
                if (transaction != null && transaction.Status == "Pending")
                {
                    transaction.Status = "Rejected";
                    await _supabase.From<TokenTransaction>().Update(transaction);

                    var employer = await _supabase.From<Employer>().Where(e => e.Id == transaction.EmployerId).Single();
                    var packageResponse = await _supabase.From<TokenPackage>().Where(p => p.Id == transaction.PackageId).Get();
                    var package = packageResponse.Models?.FirstOrDefault();
                    var packageName = package != null ? package.PackageName : "Token";

                    if (employer != null && !string.IsNullOrEmpty(employer.ProfileId))
                    {
                        var notification = new Notification
                        {
                            ProfileId = employer.ProfileId,
                            Title = "Từ chối mua Token",
                            Message = $"Yêu cầu mua gói {packageName} đã bị từ chối.",
                            CreatedAt = DateTime.UtcNow.ToString("o"),
                            IsRead = false,
                            Type = "System",
                            ActionLink = "/Employer/Wallet"
                        };
                        await _supabase.From<Notification>().Insert(notification);
                    }

                    TempData["SuccessMessage"] = "Đã từ chối giao dịch.";
                }
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction("TokenTransactions");
        }
    }
}
