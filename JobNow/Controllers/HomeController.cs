using JobNow.Models;
using JobNow.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace JobNow.Controllers
{
    public class HomeController : Controller
    {
        private readonly Supabase.Client _supabase;

        // Tiêm (Inject) Supabase Client đã cấu hình ở Program.cs vào đây
        public HomeController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();

            try
            {
                // 1. Lấy danh sách Nhà tuyển dụng (chia ra VN và World)
                var employersResponse = await _supabase.From<Employer>().Get();
                var allEmployers = employersResponse.Models;

                viewModel.TopVietNamEmployers = allEmployers.Where(e => !e.IsGlobal).Take(6).ToList();
                viewModel.TopGlobalEmployers = allEmployers.Where(e => e.IsGlobal).Take(6).ToList();

                // 2. Lấy Ngành nghề
                var industriesResponse = await _supabase.From<Industry>().Get();
                viewModel.Industries = industriesResponse.Models.Take(4).ToList();

                // 3. Lấy Địa điểm
                var locationsResponse = await _supabase.From<JobLocation>().Get();
                viewModel.Locations = locationsResponse.Models.Take(4).ToList();

                // 4. Lấy Bài viết (Cẩm nang)
                var articlesResponse = await _supabase.From<Article>().Get();
                viewModel.Articles = articlesResponse.Models.Take(3).ToList();
            }
            catch (System.Exception ex)
            {
                // Truyền trực tiếp lỗi ra ngoài View để dễ nhìn thấy
                ViewBag.ErrorMessage = ex.Message;
            }

            // Trả ViewModel ra View
            return View(viewModel);
        }
    }
}