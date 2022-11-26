namespace Mango.Services.Email.Models
{
    public class EMailLog
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Log { get; set; }
        public DateTime EmailSent { get; set; }
    }
}
