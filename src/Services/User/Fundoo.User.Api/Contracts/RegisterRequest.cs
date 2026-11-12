//using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Api.Contracts
{
    public class RegisterRequest
    {
        //[Required]
        //[StringLength(100, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        //[Required]
        //[StringLength(100, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        //[Required]
        //[EmailAddress]
        //[StringLength(256)]
        public string Email { get; set; } = string.Empty;

        //[Required]
        //[MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}