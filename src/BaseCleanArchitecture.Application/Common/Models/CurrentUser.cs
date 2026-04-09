namespace BaseCleanArchitecture.Application.Common.Models
{
    public class CurrentUser
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Permissions { get; set; } = string.Empty;
    }
}
