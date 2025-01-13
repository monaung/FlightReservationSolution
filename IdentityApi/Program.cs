
using FluentValidation;
using IdentityApi.Domain;
using IdentityApi.Features.CreateAccount;
using IdentityApi.Features.GetNewToken;
using IdentityApi.Features.LoginAccount;
using IdentityApi.Infrastructure.Data;
using IdentityApi.Infrastructure.Repository;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => 
options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddJwtAuthenticationService(builder.Configuration);
builder.Services.AddScoped<IUnitOfWork, RepositoryManagement>();
builder.Services.AddGrpc().AddJsonTranscoding();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddMediatR(c=> c.RegisterServicesFromAssemblies(typeof(Program).Assembly));
builder.Services.AddScoped(p=>
{
    var config = new TypeAdapterConfig();
    CreateAccountMapperConfig.Register(config);
    LoginAccountMapperConfig.Register(config);
    return config;
});

builder.Services.AddScoped<IMapper, ServiceMapper>();

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    await context.Database.MigrateAsync(); 
    await DatabaseSeeder.SeedAsync(context, userManager);
}
app.MapGrpcService<CreateAccountService>();
app.MapGrpcService<LoginAccountService>();
app.MapGrpcService<GetNewTokenService>();

// Configure the HTTP request pipeline.

app.Run();
