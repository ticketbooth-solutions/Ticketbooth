using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Stratis.Sidechains.Networks;
using Stratis.SmartContracts;
using Stratis.SmartContracts.CLR.Serialization;
using Ticketbooth.Scanner.Converters;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Eventing.Handlers;
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
            services.AddScoped<IQrCodeScanner, QrCodeScanner>();
            services.AddScoped<ISmartContractService, SmartContractService>();
            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<ITicketChecker, TicketChecker>();
            services.AddScoped<TicketScanStateHandler>();
            services.AddTransient<IQrCodeValidator, QrCodeValidator>();
            services.AddTransient<DetailsViewModel>();
            services.AddTransient<ScanViewModel>();
            services.AddTransient<IndexViewModel>();

            services.AddRazorPages().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new AddressConverter(network));
                JsonConvert.DefaultSettings = () => options.SerializerSettings;
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
