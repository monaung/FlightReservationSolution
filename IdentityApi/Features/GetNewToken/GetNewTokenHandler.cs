using FluentValidation;
using Grpc.Core;
using IdentityApi.Domain;
using IdentityApi.Infrastructure.Repository;
using MediatR;

namespace IdentityApi.Features.GetNewToken
{
    public record Request(string FreshToken);

    public record Response(string NewJwtToken, string NewRefreshToken);

    public class Validation : AbstractValidator<Request>
    {
        public Validation()
        {
            RuleFor(m => m.FreshToken).NotEmpty();
        }
    } 

    public record Command(Request Request) : IRequest<Response>;

    public class Handler(IUnitOfWork unitOfWork, IValidator<Request> validator): IRequestHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request.Request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).FirstOrDefault();
                throw new RpcException(new Status(StatusCode.InvalidArgument, errors!));
            }

            var _refreshToken = await unitOfWork.RefreshToken.GetRefreshTokenAsync(request.Request.FreshToken) ??
                throw new RpcException(new Status(StatusCode.NotFound, "Refresh token not found"));

            bool isRefreshTokenValid = await unitOfWork.RefreshToken.IsTokenValid(request.Request.FreshToken);

            var _user = await unitOfWork.AppUser.GetById(_refreshToken.UserId!); 

            var claims = await unitOfWork.Claim.GetClaimsAsync(_user);

            string jwtToken = unitOfWork.JwtToken.GenerateToken(claims);

            if (!isRefreshTokenValid)
            {
                string newRefreshToken = unitOfWork.RefreshToken.GenerateToken();

                var _newRefreshTokenModel = new RefreshToken
                {
                    Id = _refreshToken.Id,
                    UserId = _user!.Id,
                    Token = newRefreshToken,
                    ExpiresAt = DateTime.Now.AddHours(12)
                };

                unitOfWork.RefreshToken.UpdateToken(_newRefreshTokenModel);
                await unitOfWork.SaveChangesAsync();
                return new Response(jwtToken, newRefreshToken);
            }
            return new Response(jwtToken, _refreshToken.Token!);
        }
    }
    public class GetNewTokenHandler
    {
    }
}
