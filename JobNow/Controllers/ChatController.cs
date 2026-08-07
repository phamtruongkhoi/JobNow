using JobNow.Models;
using JobNow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JobNow.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly Supabase.Client _supabase;

        public ChatController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public async Task<IActionResult> Index()
        {
            var profileId = GetUserId();
            if (string.IsNullOrEmpty(profileId)) return RedirectToAction("Login", "Auth");

            // Load conversations where current user is either candidate or employer
            var response1 = await _supabase.From<Conversation>().Where(c => c.CandidateProfileId == profileId).Get();
            var response2 = await _supabase.From<Conversation>().Where(c => c.EmployerProfileId == profileId).Get();

            var conversations = new List<Conversation>();
            if (response1.Models != null) conversations.AddRange(response1.Models);
            if (response2.Models != null) conversations.AddRange(response2.Models);

            // Sort by last message time
            conversations = conversations.OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt).ToList();

            return View(conversations);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var profileId = GetUserId();
            if (string.IsNullOrEmpty(profileId)) return RedirectToAction("Login", "Auth");

            var convResponse = await _supabase.From<Conversation>().Where(c => c.Id == id).Single();
            var conversation = convResponse;

            if (conversation == null) return NotFound();

            // Security: Verify user is part of the conversation
            if (conversation.CandidateProfileId != profileId && conversation.EmployerProfileId != profileId)
            {
                return Forbid();
            }

            var msgResponse = await _supabase.From<Message>()
                .Where(m => m.ConversationId == id)
                .Get();
            
            var messages = msgResponse.Models?.OrderBy(m => m.CreatedAt).ToList() ?? new List<Message>();

            var vm = new ChatDetailViewModel
            {
                Conversation = conversation,
                Messages = messages,
                CurrentUserId = profileId
            };

            // Determine other user's name/logo based on who is viewing
            if (profileId == conversation.CandidateProfileId)
            {
                // Current is Candidate, Other is Employer
                var empProfileResp = await _supabase.From<Employer>().Where(e => e.ProfileId == conversation.EmployerProfileId).Get();
                var emp = empProfileResp.Models?.FirstOrDefault();
                vm.OtherUserName = emp?.Name ?? "Nhà tuyển dụng";
                vm.OtherUserLogo = emp?.LogoUrl ?? "/images/default-avatar.png";
            }
            else
            {
                // Current is Employer, Other is Candidate
                var candProfileResp = await _supabase.From<Profile>().Where(p => p.Id == conversation.CandidateProfileId).Single();
                vm.OtherUserName = candProfileResp?.FullName ?? "Ứng viên";
                vm.OtherUserLogo = candProfileResp?.AvatarUrl ?? "/images/default-avatar.png";
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> StartConversation(int jobId, string candidateProfileId)
        {
            var profileId = GetUserId();
            if (string.IsNullOrEmpty(profileId)) return RedirectToAction("Login", "Auth");

            // Get Job
            var jobResp = await _supabase.From<Job>().Where(j => j.Id == jobId).Single();
            var job = jobResp;
            if (job == null) return BadRequest("Công việc không tồn tại.");

            // Get Employer
            var empResp = await _supabase.From<Employer>().Where(e => e.Id == job.EmployerId).Single();
            var employerProfileId = empResp?.ProfileId;
            if (string.IsNullOrEmpty(employerProfileId)) return BadRequest("Nhà tuyển dụng chưa có tài khoản hợp lệ.");

            // Verify the current user is either the candidate or the employer
            if (profileId != candidateProfileId && profileId != employerProfileId)
            {
                return Forbid();
            }

            // Check if application actually exists for security
            var appResponse = await _supabase.From<Application>()
                .Where(a => a.JobId == jobId)
                .Where(a => a.ProfileId == candidateProfileId)
                .Get();
            if (appResponse.Models == null || !appResponse.Models.Any())
            {
                return BadRequest("Không tìm thấy đơn ứng tuyển hợp lệ để trò chuyện.");
            }

            // Check if conversation already exists
            var existingResp = await _supabase.From<Conversation>()
                .Where(c => c.CandidateProfileId == candidateProfileId)
                .Where(c => c.EmployerProfileId == employerProfileId)
                .Where(c => c.JobId == jobId)
                .Get();

            var existing = existingResp.Models?.FirstOrDefault();
            if (existing != null)
            {
                return RedirectToAction("Detail", new { id = existing.Id });
            }

            // Create new conversation
            var newConv = new Conversation
            {
                CandidateProfileId = candidateProfileId,
                EmployerProfileId = employerProfileId,
                JobId = jobId,
                JobTitle = job.Title ?? "Việc làm"
            };

            var insertResp = await _supabase.From<Conversation>().Insert(newConv);
            var createdConv = insertResp.Models?.FirstOrDefault();

            if (createdConv != null)
            {
                return RedirectToAction("Detail", new { id = createdConv.Id });
            }

            return BadRequest("Không thể tạo cuộc trò chuyện.");
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int conversationId, string message)
        {
            var profileId = GetUserId();
            if (string.IsNullOrEmpty(profileId)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(message))
            {
                return RedirectToAction("Detail", new { id = conversationId });
            }

            message = message.Trim();
            if (message.Length > 1000) message = message.Substring(0, 1000);

            var convResponse = await _supabase.From<Conversation>().Where(c => c.Id == conversationId).Single();
            var conversation = convResponse;
            if (conversation == null) return NotFound();

            if (conversation.CandidateProfileId != profileId && conversation.EmployerProfileId != profileId)
            {
                return Forbid();
            }

            var msg = new Message
            {
                ConversationId = conversationId,
                SenderProfileId = profileId,
                Content = message
            };

            await _supabase.From<Message>().Insert(msg);

            // Update conversation last message
            conversation.LastMessage = message;
            conversation.LastMessageAt = DateTime.UtcNow;
            await _supabase.From<Conversation>().Update(conversation);

            // Create Notification for the OTHER user
            string otherUserId = (profileId == conversation.CandidateProfileId) ? conversation.EmployerProfileId : conversation.CandidateProfileId;
            string title = (profileId == conversation.CandidateProfileId) ? "Tin nhắn mới từ ứng viên" : "Phản hồi từ Nhà tuyển dụng";
            string notifMessage = (profileId == conversation.CandidateProfileId) ? "Bạn có tin nhắn mới từ ứng viên." : "Nhà tuyển dụng đã phản hồi tin nhắn của bạn.";

            var notif = new Notification
            {
                ProfileId = otherUserId,
                Title = title,
                Message = notifMessage,
                Type = "Chat",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                ActionLink = $"/Chat/Detail/{conversationId}"
            };

            await _supabase.From<Notification>().Insert(notif);

            return RedirectToAction("Detail", new { id = conversationId });
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            var profileId = GetUserId();
            if (string.IsNullOrEmpty(profileId)) return Unauthorized();

            var convResponse = await _supabase.From<Conversation>().Where(c => c.Id == conversationId).Single();
            var conversation = convResponse;
            if (conversation == null) return NotFound();

            if (conversation.CandidateProfileId != profileId && conversation.EmployerProfileId != profileId)
            {
                return Forbid();
            }

            var msgResponse = await _supabase.From<Message>()
                .Where(m => m.ConversationId == conversationId)
                .Get();
            
            var messages = msgResponse.Models?.OrderBy(m => m.CreatedAt).Select(m => new {
                id = m.Id,
                content = m.Content,
                isMine = m.SenderProfileId == profileId,
                time = m.CreatedAt.ToString("HH:mm")
            }).ToList();

            return Json(messages);
        }
    }
}
