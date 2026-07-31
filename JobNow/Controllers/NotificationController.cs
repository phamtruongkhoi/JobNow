using JobNow.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JobNow.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly Supabase.Client _supabase;

        public NotificationController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            try
            {
                var response = await _supabase.From<Notification>().Get();

                // Lấy danh sách, lọc theo userId (hoặc null là global) và sắp xếp mới nhất lên trên
                var notifications = response.Models
                    .Where(n => string.IsNullOrEmpty(n.ProfileId) || n.ProfileId == userId)
                    .OrderByDescending(n => n.Id)
                    .ToList();

                ViewBag.TotalNotifications = notifications.Count;
                return View(notifications);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(new System.Collections.Generic.List<Notification>());
            }
        }
    }
}