using Library.API.Contexts;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

var connectionString = builder.Configuration["ConnectionStrings:LibraryDBConnectionString"];
builder.Services.AddDbContext<LibraryContext>(o => o.UseSqlServer(connectionString));

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = actionContext =>
    {
        var actionExecutingContext =
            actionContext as Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext;

        // if there are modelstate errors & all keys were correctly
        // found/parsed we're dealing with validation errors
        if (actionContext.ModelState.ErrorCount > 0
            && actionExecutingContext?.ActionArguments.Count == actionContext.ActionDescriptor.Parameters.Count)
        {
            return new UnprocessableEntityObjectResult(actionContext.ModelState);
        }

        // if one of the keys wasn't correctly found / couldn't be parsed
        // we're dealing with null/unparsable input
        return new BadRequestObjectResult(actionContext.ModelState);
    };
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setupAction =>
{
    setupAction.SwaggerDoc(
        "Lbrary.OpenAPI.Specs",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Library API",
            Version = "1",
            Description = "API for manage books and authors.",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Email = "yongshun950824@gmail.com",
                Name = "Yong Shun",
                Url = new Uri("https://github.com/yongshun950824")
            },
            License = new Microsoft.OpenApi.Models.OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        });

    string xmlCommentsFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, xmlCommentsFile);

    setupAction.IncludeXmlComments(fullPath);
});

#region Repository
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
#endregion

builder.Services.AddAutoMapper(typeof(Program).Assembly);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(setupAction =>
    {
        setupAction.SwaggerEndpoint(
            "/swagger/Lbrary.OpenAPI.Specs/swagger.json",
            "Library API");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

#region Migrate the database.  Best practice = in Main, using service scope
using (var scope = app.Services.CreateScope())
{
    try
    {
        using var context = scope.ServiceProvider.GetService<LibraryContext>();
        context?.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}
#endregion

app.Run();
