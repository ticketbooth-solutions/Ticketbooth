using Easy.MessageHub;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SmartContract.Essentials.Ciphering;
using Stratis.Sidechains.Networks;
using Stratis.SmartContracts;
using Stratis.SmartContracts.CLR.Serialization;
using System.Reflection;
using Ticketbooth.Scanner.Converters;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Messaging;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Services.Background;
using Ticketbooth.Scanner.Services.Infrastructure;
using Ticketbooth.Scanner.ViewModels;

namespace Ticketbooth.Scanner
{
    public class Startup
    {
        private readonly IHostEnvironment _environment;

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            Configuration = configuration;
            _environment = environment;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<INodeService, NodeService>();
            services.AddSingleton<IHealthChecker, HealthChecker>();
            services.AddHostedService<HealthMonitor>();

            var network = CirrusNetwork.NetworksSelector.Mainnet();
            services.AddSingleton(network);
            services.AddSingleton<IContractPrimitiveSerializer, ContractPrimitiveSerializer>();
            services.AddSingleton<ISerializer, Serializer>();
            services.AddSingleton<ITicketRepository, TicketRepository>();
            services.AddSingleton<IBlockStoreService, BlockStoreService>();
            services.AddSingleton<ISmartContractService, SmartContractService>();
            services.AddSingleton<ITicketChecker, TicketChecker>();
            services.AddSingleton<IMessageHub, MessageHub>();
            services.AddSingleton<ICipherFactory, AesCipherFactory>();
            services.AddMediatR(config => config.Using<ParallelMediator>().AsSingleton(), Assembly.GetExecutingAssembly());
            services.AddTransient<IQrCodeScanner, QrCodeScanner>();
            services.AddTransient<IQrCodeValidator, QrCodeValidator>();
            services.AddTransient<NodeViewModel>();
            services.AddTransient<DetailsViewModel>();
            services.AddTransient<ScanViewModel>();
            services.AddTransient<IndexViewModel>();

            services.AddRazorPages().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new AddressConverter(network));
                options.SerializerSettings.Converters.Add(new ByteArrayToHexConverter());
                JsonConvert.DefaultSettings = () => options.SerializerSettings;
            });

            // blazor server configures static file middleware, so have to do this in container config
            var fileExtensionProvider = new FileExtensionContentTypeProvider();
            fileExtensionProvider.Mappings.Add(".webmanifest", "application/manifest+json");
            services.Configure<StaticFileOptions>(config =>
            {
                config.ContentTypeProvider = fileExtensionProvider;
            });

            services.AddServerSideBlazor().AddCircuitOptions(o =>
            {
                if (_environment.IsDevelopment())
                {
                    o.DetailedErrors = true;
                }
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
