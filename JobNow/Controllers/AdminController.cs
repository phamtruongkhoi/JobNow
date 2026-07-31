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
    }
}
