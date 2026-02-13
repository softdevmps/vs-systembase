using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Backend.Utils
{
    public sealed class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var formParams = context.ApiDescription.ParameterDescriptions
                .Where(p => p.Source != null && p.Source.Id == "Form")
                .ToList();

            if (formParams.Count == 0)
                return;

            var hasFile = formParams.Any(p => typeof(IFormFile).IsAssignableFrom(p.Type));
            if (!hasFile)
                return;

            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>()
            };

            foreach (var param in formParams)
            {
                schema.Properties[param.Name ?? "file"] = ToSchema(param.Type);
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
            };
        }

        private static OpenApiSchema ToSchema(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;

            if (typeof(IFormFile).IsAssignableFrom(t))
            {
                return new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
            }

            if (t == typeof(string))
                return new OpenApiSchema { Type = "string" };
            if (t == typeof(int) || t == typeof(short))
                return new OpenApiSchema { Type = "integer", Format = "int32" };
            if (t == typeof(long))
                return new OpenApiSchema { Type = "integer", Format = "int64" };
            if (t == typeof(bool))
                return new OpenApiSchema { Type = "boolean" };
            if (t == typeof(decimal))
                return new OpenApiSchema { Type = "number", Format = "decimal" };
            if (t == typeof(float))
                return new OpenApiSchema { Type = "number", Format = "float" };
            if (t == typeof(double))
                return new OpenApiSchema { Type = "number", Format = "double" };

            return new OpenApiSchema { Type = "string" };
        }
    }
}
