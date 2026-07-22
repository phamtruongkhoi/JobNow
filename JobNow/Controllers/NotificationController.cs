using JobNow.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JobNow.Controllers
{
    public class NotificationController : Controller
    {
        private readonly Supabase.Client _supabase;

        public NotificationController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _supabase.From<Notification>().Get();

                // Lấy danh sách và sắp xếp mới nhất lên trên
                var notifications = response.Models.OrderByDescending(n => n.Id).ToList();

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