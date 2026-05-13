using System.ComponentModel.DataAnnotations;

namespace elasticsearch_netcore.ViewModels
{
    public class RegisterRequest
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; }
        
        [Required]
        [MinLength(6)]
        public string Password { get; set; }
        
        [EmailAddress]
        public string Email { get; set; }
    }
}
