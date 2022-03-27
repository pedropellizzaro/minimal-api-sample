using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MinimalApiSample.Data;
using MinimalApiSample.Models;
using MiniValidation;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
    options.UseSqlServer(connectionString,
    b => b.MigrationsAssembly(typeof(ApplicationContext).Assembly.FullName)));

builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RemoveSupplier", 
        policy => policy.RequireClaim("RemoveSupplier"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthConfiguration();
app.UseHttpsRedirection();

app.MapPost("/register", [AllowAnonymous] async (
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IOptions<AppJwtSettings> jwtSettings,
    RegisterUser registerUser) =>
    {
        if (registerUser is null)
            return Results.BadRequest("Usuário não informado.");

        if (!MiniValidator.TryValidate(registerUser, out var errors))
            return Results.ValidationProblem(errors);

        var user = new IdentityUser
        {
            UserName = registerUser.Email,
            Email = registerUser.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, registerUser.Password);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        var jwt = new JwtBuilder()
            .WithUserManager(userManager)
            .WithJwtSettings(jwtSettings.Value)
            .WithEmail(user.Email)
            .WithJwtClaims()
            .WithUserClaims()
            .WithUserRoles()
            .BuildUserResponse();

        return Results.Ok(jwt);
    })
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("RegisterUser")
    .WithTags("User");

app.MapPost("login", [AllowAnonymous] async (
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IOptions<AppJwtSettings> jwtSettings,
    LoginUser user) =>
{
    if (user is null)
        return Results.BadRequest("Usuário não informado.");

    if (!MiniValidator.TryValidate(user, out var errors))
        return Results.ValidationProblem(errors);

    var result = await signInManager.PasswordSignInAsync(user.Email, user.Password, true, true);

    if (result.IsLockedOut)
        return Results.BadRequest("Usuário bloqueado.");

    if (!result.Succeeded)
        return Results.BadRequest("Usuário ou senha inválidos.");

    var jwt = new JwtBuilder()
        .WithUserManager(userManager)
        .WithJwtSettings(jwtSettings.Value)
        .WithEmail(user.Email)
        .WithJwtClaims()
        .WithUserClaims()
        .WithUserRoles()
        .BuildUserResponse();

    return Results.Ok(jwt);
})
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status200OK)
    .WithName("Login")
    .WithTags("User");

app.MapGet("/supplier", [AllowAnonymous] async (ApplicationContext context) =>
    await context.Suppliers.ToListAsync())
    .WithName("ListSuppliers")
    .WithTags("Supplier");

app.MapGet("supplier/{id}", [Authorize] async (Guid id, ApplicationContext context) =>
    await context.Suppliers.FindAsync(id)
        is Supplier supplier
            ? Results.Ok(supplier)
            : Results.NotFound())
    .Produces<Supplier>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetSupplier")
    .WithTags("Supplier");

app.MapPost("/supplier", [Authorize] async (ApplicationContext context, 
    [FromBody] Supplier model) =>
    {
        if (!MiniValidator.TryValidate(model, out var errors))
            return Results.ValidationProblem(errors);

        context.Suppliers.Add(model);
        var saved = await context.SaveChangesAsync() > 0;

        return saved
            ? Results.CreatedAtRoute("GetSupplier", new { model.Id }, model)
            : Results.BadRequest("There was an error inserting supplier.");
    })
    .ProducesValidationProblem()
    .Produces<Supplier>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("InsertSupplier")
    .WithTags("Supplier");

app.MapPut("supplier/{id}", [Authorize] async (
    ApplicationContext context,
    Guid id, [FromBody] Supplier model) =>
{
    var supplier = await context.Suppliers
        .AsNoTracking<Supplier>()
        .FirstOrDefaultAsync(f => f.Id.Equals(id));

    if (supplier is null)
        return Results.NotFound();

    if (!MiniValidator.TryValidate(model, out var errors))
        return Results.ValidationProblem(errors);

    context.Suppliers.Update(model);
    var saved = await context.SaveChangesAsync() > 0;

    return saved
        ? Results.NoContent()
        : Results.BadRequest("There was an error editing supplier.");
})
.ProducesValidationProblem()
.Produces<Supplier>(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status400BadRequest)
.WithName("EditSupplier")
.WithTags("Supplier");

app.MapDelete("supplier/{id}", [Authorize] async (Guid id, ApplicationContext context) =>
{
    if (await context.Suppliers.FindAsync(id) is Supplier supplier)
    {
        context.Suppliers.Remove(supplier);
        var removed = await context.SaveChangesAsync() > 0;

        return removed
            ? Results.NoContent()
            : Results.BadRequest("There was an error removing supplier.");
    }

    return Results.NotFound();
})
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status400BadRequest)
.RequireAuthorization("RemoveSupplier")
.WithName("RemoveSupplier")
.WithTags("Supplier");

app.Run();