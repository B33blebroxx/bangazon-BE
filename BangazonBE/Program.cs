using BangazonBE.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Hosting;




// allows passing datetimes without time zone data 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
// allows our api endpoints to access the database through Entity Framework Core
builder.Services.AddNpgsql<BangazonBEDbContext>(builder.Configuration["BangazonBEDbConnectionString"]);

// Set the JSON serializer options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//Post new user
app.MapPost("/users", (BangazonBEDbContext db, User newUser) =>
{
    try
    {
        db.Users.Add(newUser);
        db.SaveChanges();
        return Results.Created($"/user/{newUser.Id}", newUser);
    }
    catch (DbUpdateException)
    {
        return Results.BadRequest("Couldn't create new user, please try again!");
    }
});

//Get all users
app.MapGet("/users", (BangazonBEDbContext db) =>
{
    return db.Users.ToList();
});

//Get user by id
app.MapGet("/users/{id}", (BangazonBEDbContext db, int id) =>
{
    User chosenUser = db.Users.FirstOrDefault(u => u.Id == id);
    if (chosenUser == null)
    {
        return Results.NotFound("User not found.");
    }
    return Results.Ok(chosenUser);
});

//Search for user by name
app.MapGet("/users/name-search", (BangazonBEDbContext db, string query) =>
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return Results.BadRequest("Search query cannot be empty");
    }
    query = query.ToLower();

    // Filter users whose first name or last name matches the query
    var filteredUsers = db.Users
         .Where(u => (u.FirstName.ToLower() + " " + u.LastName.ToLower()).Contains(query))
         .ToList();

    if (filteredUsers.Count == 0)
    {
        return Results.NotFound("No users found with that name.");
    }
    else
    {
        return Results.Ok(filteredUsers);
    }
});

app.Run();

