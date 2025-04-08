using Avancira.Application.Identity.Roles.Dtos;
using FluentValidation;

namespace Avancira.Application.Identity.Roles.Validators;
public class UpdatePermissionsValidator : AbstractValidator<UpdatePermissionsDto>
{
    public UpdatePermissionsValidator()
    {
        RuleFor(r => r.RoleId)
            .NotEmpty();
        RuleFor(r => r.Permissions)
            .NotNull();
    }
}