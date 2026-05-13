using System.ComponentModel.DataAnnotations;

namespace elasticsearch_netcore.ViewModels
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }
        
        [Required]
        public string Password { get; set; }
    }
}
