using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApiSample.Data;
using MinimalApiSample.Models;
using MiniValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/supplier", async (ApplicationContext context) =>
    await context.Suppliers.ToListAsync())
    .WithName("ListSuppliers")
    .WithTags("Supplier");

app.MapGet("supplier/{id}", async (Guid id, ApplicationContext context) =>
    await context.Suppliers.FindAsync(id)
        is Supplier supplier
            ? Results.Ok(supplier)
            : Results.NotFound())
    .Produces<Supplier>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetSupplier")
    .WithTags("Supplier");

app.MapPost("/supplier", async (ApplicationContext context, [FromBody] Supplier model) =>
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

app.MapPut("supplier/{id}", async (
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

app.MapDelete("supplier/{id}", async (Guid id, ApplicationContext context) =>
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
.WithName("RemoveSupplier")
.WithTags("Supplier");

app.Run();