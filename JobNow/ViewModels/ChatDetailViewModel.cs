using JobNow.Models;
using System.Collections.Generic;

namespace JobNow.ViewModels
{
    public class ChatDetailViewModel
    {
        public Conversation Conversation { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
        public string CurrentUserId { get; set; }
        public string OtherUserName { get; set; }
        public string OtherUserLogo { get; set; }
    }
}
