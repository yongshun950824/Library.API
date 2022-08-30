using Library.API.Models;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Library.API.OperationFilters
{
    public class CreateBookOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.OperationId != "CreateBook")
                return;

            var schema = context.SchemaGenerator.GenerateSchema(typeof(BookForCreationWithAmountOfPages), context.SchemaRepository);

            operation.RequestBody.Content.Add(
                "application/vnd.marvin.bookforcreationwithamountofpages+json",
                new OpenApiMediaType()
                {
                    Schema = schema
                });
        }
    }
}
