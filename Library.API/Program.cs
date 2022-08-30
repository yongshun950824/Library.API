using Library.API.Contexts;
using Library.API.OperationFilters;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(setupAction =>
{
    #region Set ProducesResponseTypeAttribute in global API level
    //setupAction.Filters
    //    .Add(new ProducesResponseTypeAttribute(StatusCodes.Status400BadRequest));

    //setupAction.Filters
    //    .Add(new ProducesResponseTypeAttribute(StatusCodes.Status406NotAcceptable));

    //setupAction.Filters
    //    .Add(new ProducesResponseTypeAttribute(StatusCodes.Status500InternalServerError));
    #endregion

    setupAction.ReturnHttpNotAcceptable = true;

    setupAction.OutputFormatters
        .Add(new XmlSerializerOutputFormatter());

    var jsonOutputFormatter = setupAction.OutputFormatters
        .OfType<SystemTextJsonOutputFormatter>().FirstOrDefault();

    if (jsonOutputFormatter != null)
    {
        // remove text/json as it isn't the approved media type
        // for working with JSON at API level
        if (jsonOutputFormatter.SupportedMediaTypes.Contains("text/json"))
        {
            jsonOutputFormatter.SupportedMediaTypes.Remove("text/json");
        }
    }
});

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

    setupAction.OperationFilter<GetBookOperationFilter>();
    setupAction.OperationFilter<CreateBookOperationFilter>();

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

app.UseStaticFiles();

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
