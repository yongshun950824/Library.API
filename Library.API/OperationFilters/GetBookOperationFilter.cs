using Library.API.Models;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Library.API.OperationFilters
{
    public class GetBookOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.OperationId != "GetBook")
                return;

            // StackOverflow reference: https://stackoverflow.com/questions/65455791/ioperation-filter-change-in-swashbuckle-v5-5-3
            var schema = context.SchemaGenerator.GenerateSchema(typeof(BookWithConcatenatedAuthorName), context.SchemaRepository);

            operation.Responses[StatusCodes.Status200OK.ToString()].Content.Add(
                "application/vnd.marvin.bookwithconcatenatedauthorname+json",
                new OpenApiMediaType()
                {
                    // Archive
                    // Schema = context.SchemaRegistry.GetOrRegister(typeof(BookWithConcatenatedAuthorName))
                    Schema = schema
                });
        }
    }
}
