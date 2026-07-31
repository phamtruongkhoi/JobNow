using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Supabase.Gotrue;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using static JobNow.ViewModels.HomeViewModel;

namespace JobNow.Controllers
{
    public class AuthController : Controller
    {
        private readonly Supabase.Client _supabase;

        public AuthController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        // 1. MÀN HÌNH ĐĂNG NHẬP
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            try
            {
                // Gọi Supabase đăng nhập
                var session = await _supabase.Auth.SignIn(model.Email, model.Password);

                // Lấy thông tin Role từ DB
                var response = await _supabase.From<Models.Profile>()
                    .Where(x => x.Id == session.User.Id)
                    .Single();
                
                var role = response?.Role ?? "candidate";

                // Tạo Cookie chứng nhận đã đăng nhập cho ASP.NET
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.NameIdentifier, session.User.Id),
                    new Claim(ClaimTypes.Email, session.User.Email),
                    new Claim(ClaimTypes.Role, role)
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                if (role == "admin") {
                    return RedirectToAction("Index", "Admin");
                } else if (role == "employer") {
                    return RedirectToAction("Index", "Employer");
                } else {
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = "Sai email hoặc mật khẩu!";
                return View();
            }
        }

        // 2. MÀN HÌNH ĐĂNG KÝ
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            try
            {
                // Gọi Supabase đăng ký
                var session = await _supabase.Auth.SignUp(model.Email, model.Password);

                // Lưu tạm thông tin Tên và Role vào TempData để sau khi nhập OTP sẽ lưu vào DB
                TempData["RegName"] = model.Name;
                TempData["RegRole"] = model.Role;

                // Chuyển sang trang nhập OTP
                return RedirectToAction("VerifyOtp", new { email = model.Email, type = "signup" });
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }

        // 3. MÀN HÌNH NHẬP MÃ OTP
        [HttpGet]
        public IActionResult VerifyOtp(string email, string type)
        {
            return View(new VerifyOtpVM { Email = email, Type = type });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpVM model)
        {
            try
            {
                var otpType = model.Type == "signup" ? Constants.EmailOtpType.Signup : Constants.EmailOtpType.Recovery;

                // Thêm .Trim() để triệt tiêu các khoảng trắng bị dư do copy/paste
                var session = await _supabase.Auth.VerifyOTP(model.Email.Trim(), model.Otp.Trim(), otpType);

                if (model.Type == "signup")
                {
                    // Nếu là đăng ký, chèn dữ liệu vào bảng jn_profiles
                    var name = TempData["RegName"]?.ToString();
                    var role = TempData["RegRole"]?.ToString();

                    // Đoạn code MỚI dùng Model chuẩn:
                    var profile = new Models.Profile
                    {
                        Id = session.User.Id,
                        FullName = name,
                        Role = role
                    };
                    await _supabase.From<Models.Profile>().Insert(profile);

                    TempData["Success"] = "Đăng ký thành công! Hãy đăng nhập.";
                    return RedirectToAction("Login");
                }
                else
                {
                    // Nếu là quên mật khẩu -> Cho phép đổi pass luôn (Chuyển đến trang Reset)
                    // ... (Cậu có thể code thêm view Đổi pass tại đây)
                    return RedirectToAction("Login");
                }
            }
            catch (System.Exception ex)
            {
                // In thẳng lỗi từ Supabase ra màn hình để bắt đúng bệnh
                ViewBag.Error = $"Lỗi Supabase: {ex.Message}";
                return View(model);
            }
        }

        // 6. XỬ LÝ GỬI LẠI MÃ OTP
        [HttpGet]
        public async Task<IActionResult> ResendOtp(string email, string type)
        {
            try
            {
                if (type == "signup")
                {
                    // LƯU Ý: Thư viện Supabase C# SDK hiện tại không có hàm Resend giống JavaScript.
                    // Để tránh lỗi biên dịch CS1061, tạm thời không gọi lệnh này.
                    // await _supabase.Auth.Resend(email, Supabase.Gotrue.Constants.EmailOtpType.Signup);

                    TempData["Success"] = "Tính năng gửi lại mã đăng ký chưa được hỗ trợ ở phiên bản thư viện hiện tại.";
                }
                else
                {
                    // Gửi lại mã Quên mật khẩu (Hàm này thì C# SDK có hỗ trợ)
                    await _supabase.Auth.ResetPasswordForEmail(email);
                    TempData["Success"] = "Đã gửi lại mã OTP mới vào email của bạn! Vui lòng kiểm tra.";
                }
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = $"Không thể gửi lại mã: {ex.Message}";
            }
            return RedirectToAction("VerifyOtp", new { email = email, type = type });
        }

        // ĐĂNG XUẤT
        public async Task<IActionResult> Logout()
        {
            await _supabase.Auth.SignOut();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // 4. MÀN HÌNH QUÊN MẬT KHẨU (Nhập Email)
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            try
            {
                // Yêu cầu Supabase gửi mã OTP khôi phục
                await _supabase.Auth.ResetPasswordForEmail(email);
                return RedirectToAction("VerifyOtp", new { email = email, type = "recovery" });
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = "Lỗi: " + ex.Message;
                return View();
            }
        }

        // 5. MÀN HÌNH ĐẶT LẠI MẬT KHẨU MỚI (Sau khi nhập đúng OTP)
        [HttpGet]
        public IActionResult ResetPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword)
        {
            try
            {
                // Cập nhật mật khẩu mới
                var attrs = new Supabase.Gotrue.UserAttributes { Password = newPassword };
                await _supabase.Auth.Update(attrs);

                // Đăng xuất và bắt đăng nhập lại bằng mật khẩu mới
                await _supabase.Auth.SignOut();
                TempData["Success"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = "Lỗi: " + ex.Message;
                return View();
            }
        }
    }
}