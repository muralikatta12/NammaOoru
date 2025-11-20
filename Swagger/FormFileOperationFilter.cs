using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using NammaOoru.Models;

namespace NammaOoru.Swagger
{
    // Adds proper multipart/form-data description for endpoints that accept UploadPhotoRequest
    public class FormFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasUpload = context.MethodInfo.GetParameters()
                .Any(p => p.ParameterType == typeof(UploadPhotoRequest) ||
                          p.GetCustomAttributes().OfType<FromFormAttribute>().Any());

            if (!hasUpload) return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["file"] = new OpenApiSchema { Type = "string", Format = "binary" },
                                ["caption"] = new OpenApiSchema { Type = "string" },
                                ["isPrimary"] = new OpenApiSchema { Type = "boolean" }
                            },
                            Required = new HashSet<string> { "file" }
                        }
                    }
                }
            };
        }
    }
}
