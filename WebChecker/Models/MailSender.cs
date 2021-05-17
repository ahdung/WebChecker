namespace AhDung.WebChecker.Models
{
    public class MailSender
    {
        public string Address{ get; set; }

        public string SmtpServer{ get; set; }

        public int Port{ get; set; }

        public bool UseSsl{ get; set; }

        public string User { get; set; }

        public string Password { get; set; }
    }
}
