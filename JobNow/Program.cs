using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Supabase;

namespace JobNow
{
    public class Program
    {
        // Nhớ đổi void thành async Task để có thể dùng await cho Supabase nhé
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // 1. Đọc cấu hình từ appsettings.json
            var supabaseUrl = builder.Configuration["Supabase:Url"];
            var supabaseKey = builder.Configuration["Supabase:Key"];

            // 2. Cấu hình Option cho Supabase
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            // 3. Khởi tạo Supabase Client
            var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);
            await supabaseClient.InitializeAsync(); // Bắt buộc phải có dòng này để khởi tạo Auth

            // 4. Đăng ký Supabase vào hệ thống DI dạng Singleton
            builder.Services.AddSingleton(supabaseClient);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}