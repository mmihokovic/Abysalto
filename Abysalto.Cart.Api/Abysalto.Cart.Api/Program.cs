using Abysalto.Cart.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CartDb")
    ?? throw new InvalidOperationException("Connection string 'CartDb' is missing.");

builder.Services.AddDbContext<CartDbContext>(
    options => options.UseSqlServer(connectionString));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options => options.MapInboundClaims = false);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CartOwner", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context =>
            context.User.FindFirst("sub")?.Value is { Length: > 0 and <= 200 }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
