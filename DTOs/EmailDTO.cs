using System.ComponentModel.DataAnnotations;

namespace ShuttlOps.DTOs
{
    public class EmailDTO
    {
        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsHtml { get; set; } = true;

        public List<string> EmailRecipients { get; set; } = new();
    }
}
