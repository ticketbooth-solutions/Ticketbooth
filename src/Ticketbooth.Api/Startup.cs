using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartContract.Essentials.Ciphering;
using SmartContract.Essentials.Randomness;
using Swashbuckle.AspNetCore.SwaggerGen;
using Ticketbooth.Api.Converters;

namespace Ticketbooth.Api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {

            // web server
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddApplicationPart(typeof(TicketboothApiFeature).Assembly)
                .AddFluentValidation(config =>
                {
                    config.RegisterValidatorsFromAssemblyContaining<TicketboothApiFeature>();
                    config.ImplicitlyValidateChildProperties = true;
                    config.RunDefaultMvcValidationAfterFluentValidationExecutes = false;
                });

            // api dependencies
            services.AddVersionedApiExplorer(options =>
            {
                // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                // note: the specified format code will format the version as "'v'major[.minor][-status]"
                options.GroupNameFormat = "'v'VVV";

                // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                options.SubstituteApiVersionInUrl = true;
                options.SubstitutionFormat = "VV";
            });
            services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
            });
            services.AddTransient<IStringGenerator, UrlFriendlyStringGenerator>();
            services.AddTransient<ICipherFactory, AesCipherFactory>();

            // serialization
            services.AddSingleton<AddressConverter>();
            services.AddSingleton<ByteArrayToHexConverter>();
            services.AddTransient<IConfigureOptions<MvcJsonOptions>, TicketboothJsonOptions>();

            // swagger
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IApiVersionDescriptionProvider provider)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = string.Empty;
                c.DocumentTitle = "Ticketbooth Full Node API";

                foreach (var description in provider.ApiVersionDescriptions)
                {
                    c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"Ticketbooth V{description.ApiVersion.ToString()}");
                }
            });

            app.UseMvc();
        }
    }
}
