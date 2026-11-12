
using FluentValidation;
using Fundoo.User.Api.Contracts;

namespace Fundoo.User.Api.Validation
{
    public sealed class RegisterRequestValidator: AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(request => request.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(request => request.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(request => request.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(256);

            RuleFor(request => request.Password)
                .NotEmpty()
                .MinimumLength(8);
        }
    }
}
