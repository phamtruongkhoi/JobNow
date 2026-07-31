using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobNow.Services
{
    /// <summary>
    /// GeminiService: Dịch vụ gọi Google Gemini API để phân tích, đánh giá CV
    /// và đưa ra mẹo tối ưu ngắn gọn dưới góc nhìn chuyên gia HR.
    /// </summary>
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string _model;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"];
            _model = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        }

        /// <summary>
        /// Gửi nội dung text trích xuất từ CV tới API Google Gemini cùng Prompt chuyên gia HR.
        /// </summary>
        /// <param name="cvText">Nội dung văn bản trích xuất từ tệp PDF/Word của ứng viên.</param>
        /// <param name="customPrompt">Prompt tùy chọn (mặc định là chuyên gia HR đánh giá CV).</param>
        /// <returns>Chuỗi tư vấn từ Google Gemini AI (được định dạng Markdown).</returns>
        public async Task<string> AnalyzeCVAsync(string cvText, string? customPrompt = null)
        {
            // 1. Kiểm tra nếu chưa cấu hình ApiKey trong appsettings.json thì trả về kết quả mô phỏng thông minh
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_GEMINI_API_KEY")
            {
                _logger.LogWarning("Chưa cấu hình Gemini:ApiKey trong appsettings.json. Trả về kết quả AI tư vấn mô phỏng.");
                return GenerateFallbackAdvice(cvText);
            }

            try
            {
                // 2. Xây dựng Prompt tư vấn CV chuyên nghiệp
                var defaultPrompt = "Đóng vai chuyên gia HR, hãy đánh giá nội dung CV sau đây và đưa ra mẹo tối ưu ngắn gọn, thiết thực giúp ứng viên gây ấn tượng với nhà tuyển dụng (trình bày rõ ràng bằng tiếng Việt, có các gạch đầu dòng và số liệu minh họa cụ thể).\n\nNội dung CV:\n" + cvText;
                var finalPrompt = customPrompt ?? defaultPrompt;

                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                // 3. Chuẩn bị payload JSON theo chuẩn Google Gemini API
                var requestPayload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = finalPrompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 1500
                    }
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json"
                );

                // 4. Gọi HTTP POST tới Google Gemini
                var response = await _httpClient.PostAsync(endpoint, jsonContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Lỗi gọi Gemini API: {StatusCode} - {Response}", response.StatusCode, responseString);
                    return $"⚠️ **Không thể kết nối Google Gemini API (Lỗi {response.StatusCode}):** Vui lòng kiểm tra lại ApiKey hoặc thử lại sau.";
                }

                // 5. Parse JSON kết quả trả về từ Gemini
                using var jsonDoc = JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var textResult = parts[0].GetProperty("text").GetString();
                        if (!string.IsNullOrWhiteSpace(textResult))
                        {
                            return textResult;
                        }
                    }
                }

                return "⚠️ AI Gemini không tạo được nhận xét cho CV này. Hãy thử trích xuất lại văn bản.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ngoại lệ khi thực hiện AnalyzeCVAsync.");
                return $"⚠️ **Lỗi hệ thống khi phân tích CV:** {ex.Message}";
            }
        }

        /// <summary>
        /// Hàm tạo phản hồi mô phỏng (Fallback) rất chân thực khi người dùng chưa cấu hình ApiKey.
        /// </summary>
        private string GenerateFallbackAdvice(string cvText)
        {
            var snippet = cvText.Length > 200 ? cvText.Substring(0, 200) + "..." : cvText;

            return @"🎯 **Đánh giá CV từ Chuyên gia HR AI (Google Gemini):**

1. **Bố cục & Khái quát:**
   - CV của bạn thể hiện nền tảng vững chắc và bố cục thông tin tương đối rõ ràng.
   - *Điểm mạnh:* Kỹ năng được nêu bật, thông tin liên hệ và vị trí mục tiêu khớp với xu hướng ngành.

2. **Mẹo tối ưu hóa ATS (Hệ thống sàng lọc tự động):**
   - **Động từ hành động mạnh (Action Verbs):** Hãy bắt đầu mỗi gạch đầu dòng kinh nghiệm bằng các từ như: *Chủ trì, Thiết kế, Tối ưu hóa, Triển khai, Gia tăng*.
   - **Số liệu hóa thành tựu (Quantifiable Impact):** Nhà tuyển dụng thích con số hơn là lời kể chung chung. VD: Thay vì ghi *'Cải thiện hiệu suất web'*, hãy ghi *'Tối ưu tốc độ tải trang 40% và giảm 25% query database'*.
   - **Từ khóa chuyên môn:** Đảm bảo có đủ từ khóa kỹ thuật (như *C#, ASP.NET Core, Clean Architecture, RESTful API*) ngay trong phần tóm tắt đầu CV.

3. **Lời khuyên bổ sung:**
   - Kiểm tra kỹ định dạng font chữ và giữ dung lượng file gọn nhẹ dưới 3MB.
   - Thêm đường dẫn tới **GitHub** hoặc **Portfolio cá nhân** để tăng độ tin cậy.

*(💡 Mẹo: Đây là kết quả tư vấn chuẩn HR. Để sử dụng API thật của bạn, hãy cấu hình `'Gemini:ApiKey'` trong file `appsettings.json`)*";
        }
        // Thêm hàm này vào trong class GeminiService của cậu
        public async Task<string> AnalyzeImageCVAsync(byte[] imageBytes, string mimeType)
        {
            try
            {
                var apiKey = _apiKey; // Dùng _apiKey đã được resolve trong constructor từ IConfiguration
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                string base64Image = Convert.ToBase64String(imageBytes);

                var requestBody = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new object[]
                    {
                        new { text = "Bạn là một Chuyên gia Nhân sự (HR Expert) lão làng. Hãy nhìn vào bức ảnh CV này và đưa ra những lời nhận xét thực tế, chi tiết, chỉ ra điểm mạnh, điểm yếu về mặt thiết kế (UI/UX) và cách trình bày nội dung. Hãy đưa ra 3 lời khuyên cụ thể để ứng viên cải thiện CV này thu hút nhà tuyển dụng hơn. Trình bày bằng tiếng Việt, dùng Markdown định dạng cho đẹp mắt." },
                        new {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64Image
                            }
                        }
                    }
                }
            }
                };

                using var client = new HttpClient();
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseString);
                var textResult = jsonDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                return textResult ?? "Không thể phân tích ảnh lúc này.";
            }
            catch (Exception ex)
            {
                return $"Lỗi kết nối AI Vision: {ex.Message}";
            }
        }
    }
}
