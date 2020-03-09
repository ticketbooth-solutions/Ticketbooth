using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Ticketbooth.Api
{
    public class AlphabeticOrderFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = swaggerDoc.Paths.OrderBy(e => e.Key);
            swaggerDoc.Paths = paths.ToDictionary(e => e.Key, e => e.Value);
        }
    }
}
