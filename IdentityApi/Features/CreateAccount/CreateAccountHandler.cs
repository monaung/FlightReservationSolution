using FluentValidation;
using Grpc.Core;
using IdentityApi.Domain;
using IdentityApi.Infrastructure.Repository;
using Mapster;
using MapsterMapper;
using MediatR;
using Shared.Authentication;
using System.Security.Claims;
using IdentityApi.Protos;
using IdentityApi.Features.LoginAccount;

namespace IdentityApi.Features.CreateAccount
{
    public class Request
    {
        public string? Fullname { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }

    public class Validation: AbstractValidator<Request>
    {
        public Validation()
        {
            RuleFor(m => m.Email).NotEmpty().EmailAddress();
            RuleFor(m => m.Password).NotEmpty()
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,15}$")
                .WithMessage("Password must be between 8 and 15 characters and contain at least one lowercase letter, one uppercase letter, one numeric digit, and one special character.");
            RuleFor(m => m.ConfirmPassword).Equal(m => m.Password).WithMessage("Password and Confirm Password must match.");
        }
    }

    internal static class CreateAccountMapperConfig
    {
        public static void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Request, AppUser>()
                .Map(d => d.PasswordHash, s => s.Password)
                .Map(d => d.UserName, s => s.Email);
        }
    }

    public record Command(Request Account): IRequest<bool>;

    internal class Handler(IUnitOfWork unitOfWork, IValidator<Request> validator, IMapper mapper)
        : IRequestHandler<Command, bool>
    {
        public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request.Account, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors)));
            }

            var mapData = mapper.From(request.Account).AdaptToType<AppUser>();
            await unitOfWork.AppUser.CreateAsync(mapData);

            var _user = await unitOfWork.AppUser.GetByEmail(mapData.Email!);

            List<Claim> claims = [
                    new(ClaimTypes.Role, Roles.User),
                    new (ClaimTypes.Email, _user.Email!),
                    new (ClaimTypes.Name, _user.FullName!),
                    new(PolicyNames.key, PolicyNames.UserPolicy),
                    new(Permissions.CanRead, false.ToString()),
                    new(Permissions.CanUpdate, false.ToString()),
                    new(Permissions.CanDelete, false.ToString()),
                    new(Permissions.CanCreate, false.ToString())
                ];

            await unitOfWork.Claim.AssignClaims(_user, claims);

            return true;
        }
    }

    internal class CreateAccountService(ISender sender): CreateAccountServiceProto.CreateAccountServiceProtoBase
    {
        public override async Task<CreateAccountResponseProto> CreateAccountProto(CreateAccountRequestProto request, 
            ServerCallContext context)
        {
            var mapRequest = request.Adapt<Request>();
            var result = await sender.Send(new Command(mapRequest));
            return new CreateAccountResponseProto { Success = result };
        }
    }
}
