using Library.API.Authentication;
using Library.API.Contexts;
using Library.API.OperationFilters;
using Library.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(setupAction =>
{
    #region Set ProducesResponseTypeAttribute in global API level
    setupAction.Filters
        .Add(new ProducesResponseTypeAttribute(StatusCodes.Status400BadRequest));

    setupAction.Filters
        .Add(new ProducesResponseTypeAttribute(StatusCodes.Status406NotAcceptable));

    setupAction.Filters
        .Add(new ProducesResponseTypeAttribute(StatusCodes.Status500InternalServerError));

    setupAction.Filters
        .Add(new ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized));

    setupAction.Filters
        .Add(new ProducesDefaultResponseTypeAttribute());
    #endregion

    setupAction.Filters.Add(new AuthorizeFilter());

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

builder.Services.AddVersionedApiExplorer(setupAction =>
{
    setupAction.GroupNameFormat = "'v'VV";
});

builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

builder.Services.AddApiVersioning(setupAction =>
{
    setupAction.AssumeDefaultVersionWhenUnspecified = true;
    setupAction.DefaultApiVersion = new ApiVersion(1, 0);
    setupAction.ReportApiVersions = true;

    // Version in header
    //setupAction.ApiVersionReader = new HeaderApiVersionReader("api-version");

    // Version in media type
    //setupAction.ApiVersionReader = new MediaTypeApiVersionReader();
});

var apiVersionDescriptionProvider = builder.Services
    .BuildServiceProvider()
    .GetService<IApiVersionDescriptionProvider>()!;

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setupAction =>
{
    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
    {
        setupAction.SwaggerDoc(
            $"Lbrary.OpenAPI.Specs.{description.GroupName}",
            new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Library API Spec.",
                Version = description.ApiVersion.ToString(),
                Description = "API for access authors and books.",
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
    }

    //setupAction.SwaggerDoc(
    //    "Lbrary.OpenAPI.Specs.Authors",
    //    new Microsoft.OpenApi.Models.OpenApiInfo
    //    {
    //        Title = "Library API Spec. (Authors)",
    //        Version = "1",
    //        Description = "API for access authors.",
    //        Contact = new Microsoft.OpenApi.Models.OpenApiContact
    //        {
    //            Email = "yongshun950824@gmail.com",
    //            Name = "Yong Shun",
    //            Url = new Uri("https://github.com/yongshun950824")
    //        },
    //        License = new Microsoft.OpenApi.Models.OpenApiLicense
    //        {
    //            Name = "MIT License",
    //            Url = new Uri("https://opensource.org/licenses/MIT")
    //        }
    //    });

    //setupAction.SwaggerDoc(
    //    "Lbrary.OpenAPI.Specs.Books",
    //    new Microsoft.OpenApi.Models.OpenApiInfo
    //    {
    //        Title = "Library API Spec. (Books)",
    //        Version = "1",
    //        Description = "API for access books.",
    //        Contact = new Microsoft.OpenApi.Models.OpenApiContact
    //        {
    //            Email = "yongshun950824@gmail.com",
    //            Name = "Yong Shun",
    //            Url = new Uri("https://github.com/yongshun950824")
    //        },
    //        License = new Microsoft.OpenApi.Models.OpenApiLicense
    //        {
    //            Name = "MIT License",
    //            Url = new Uri("https://opensource.org/licenses/MIT")
    //        }
    //    });

    setupAction.AddSecurityDefinition("basicAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Require Username and Password to access."
    });

    setupAction.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "basicAuth"
                }
            }, new List<string> {}
        }
    });

    setupAction.DocInclusionPredicate((documentName, apiDescription) =>
    {
        var actionApiVersionModel = apiDescription.ActionDescriptor
            .GetApiVersionModel(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit);

        if (actionApiVersionModel == null)
        {
            return true;
        }

        if (actionApiVersionModel.DeclaredApiVersions.Any())
        {
            return actionApiVersionModel.DeclaredApiVersions.Any(v =>
                $"Lbrary.OpenAPI.Specs.v{v}" == documentName);
        }

        return actionApiVersionModel.ImplementedApiVersions.Any(v =>
            $"Lbrary.OpenAPI.Specs.v{v}" == documentName);
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
        setupAction.InjectStylesheet("/Assets/custom-ui.css");
        setupAction.IndexStream = () => Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Library.API.EmbeddedAssets.index.html");

        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            setupAction.SwaggerEndpoint($"/swagger/" +
                $"Lbrary.OpenAPI.Specs.{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }

        //setupAction.SwaggerEndpoint(
        //    "/swagger/Lbrary.OpenAPI.Specs.Authors/swagger.json",
        //    "Library API (Authors)");

        //setupAction.SwaggerEndpoint(
        //    "/swagger/Lbrary.OpenAPI.Specs.Books/swagger.json",
        //    "Library API (Books)");

        setupAction.DefaultModelExpandDepth(2);
        setupAction.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
        setupAction.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        setupAction.EnableDeepLinking();
        setupAction.DisplayOperationId();
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();

//app.UseAuthorization();

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
