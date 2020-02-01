using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using SmartContract.Essentials.Ciphering;
using SmartContract.Essentials.Randomness;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Features.SmartContracts;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IO;
using System.Reflection;

namespace Ticketbooth.Api
{
    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class FullNodeBuilderExtension
    {
        /// <summary>
        /// Adds the Ticketbooth API to the full node
        /// </summary>
        public static IFullNodeBuilder AddTicketboothApi(this IFullNodeBuilder fullNodeBuilder)
        {
            return fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<TicketboothApiFeature>()
                    .DependOn<SmartContractFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddTransient<IStringGenerator, UrlFriendlyStringGenerator>();
                        services.AddTransient<ICipherFactory, AesCipherFactory>();

                        services.Configure<IMvcBuilder>(builder =>
                        {
                            // add validation
                            builder.AddFluentValidation(config =>
                            {
                                config.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
                                config.ImplicitlyValidateChildProperties = true;
                            });
                        });

                        services.Configure<SwaggerGenOptions>(config =>
                        {
                            // enables request/response examples
                            config.OperationFilter<ExamplesOperationFilter>();

                            // Swagger documentation
                            var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                            config.IncludeXmlComments(Path.Combine(basePath, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
                            config.AddFluentValidationRules();
                        });
                    });
            });
        }
    }
}
