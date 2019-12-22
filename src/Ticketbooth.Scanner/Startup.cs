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
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.Services;
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
            var network = CirrusNetwork.NetworksSelector.Mainnet();
            services.AddSingleton(network);
            services.AddSingleton<IContractPrimitiveSerializer, ContractPrimitiveSerializer>();
            services.AddSingleton<ISerializer, Serializer>();
            services.AddScoped<ISmartContractService, SmartContractService>();
            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<ITicketChecker, TicketChecker>();
            services.AddTransient<QrCodeValidator>();
            services.AddTransient<DetailsViewModel>();
            services.AddScoped<AppStateViewModel>();

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
