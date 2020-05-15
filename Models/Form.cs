using System.ComponentModel.DataAnnotations;

namespace Rooms.Models
{
    public class RegForm
    {
        [Required, StringLength(34, MinimumLength = 4)]
        public string Name { get; set; }
        [Required, StringLength(320, MinimumLength = 6), EmailAddress]
        public string Email { get; set; }
        [Required, StringLength(16, MinimumLength = 6), DataType(DataType.Password)]
        public string Password { get; set; }
    }
    public class SignInForm
    {
        [Required, StringLength(320, MinimumLength = 6)]
        public string Email { get; set; }
        [Required, StringLength(16, MinimumLength = 6)]
        public string Password { get; set; }
    }
    public class RoomForm
    {
        [Required, StringLength(34, MinimumLength = 4)]
        public string Name { get; set; }
        [MaxLength(200)]
        public string Description { get; set; }
        [Required, StringLength(2, MinimumLength = 2)]
        public string Country { get; set; }
        [StringLength(16, MinimumLength = 6)]
        public string Password { get; set; }
        [Required, Range(2, 15)]
        public byte Limit { get; set; }
    }
    public class UserChangeForm
    {
        [StringLength(34, MinimumLength = 4)]
        public string Name { get; set; }
        [StringLength(16, MinimumLength = 6), DataType(DataType.Password)]
        public string Password { get; set; }
    }
}