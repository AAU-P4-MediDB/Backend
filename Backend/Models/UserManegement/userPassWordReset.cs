namespace Backend.Models
{
    public class UserPassWordResetRequest
    {
        public string email { get; set; } = null!;
        public string new_pass { get; set; } = null!;
    }
        
    
}