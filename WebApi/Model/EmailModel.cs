namespace WebApi.Model
{
    public class EmailModel
    {
        public string SenderEmail { get; set; }
        public string SenderPassword { get; set; }

        public string SmtpClient { get; set; }

        public int SmtpPort { get; set; }
        public string EmailBody { get; set; }
        public string EmailSubject { get; set; }
        public string QuizReminderEmailSubject { get; set; }
        public string QuizReminderEmailBodyTemplate { get; set; }

        public string CourseReminderEmailSubject { get; set; }

        public string CourseReminderEmailBodyTemplate { get; set; }


        //public string Email { get; set; }
    }
}
