using APICore.API.Authorization;
using APICore.API.Filters;
using APICore.API.Middleware;
using APICore.API.Middlewares;
using APICore.API.Utils;
using APICore.Data.Repository;
using APICore.Data.UoW;
using APICore.Services;
using APICore.Services.Impls;
using APICore.Services.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace APICore.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Information()
                         .WriteTo.File("logs/apicore-.log", rollingInterval: RollingInterval.Day)
                         .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureI18N();
            services.ConfigureCors();
            services.AddMvc(config =>
            {
                config.Filters.Add(typeof(ApiValidationFilterAttribute));
                config.EnableEndpointRouting = false;
            }).AddNewtonsoftJson(opt => opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            services.ConfigureHsts();

            services.ConfigureDbContext(Configuration);
            services.ConfigureSwagger();
            services.ConfigureTokenAuth(Configuration);
            services.ConfigurePerformance();

            services.ConfigureHealthChecks(Configuration);
            services.ConfigureDetection();

            services.AddHttpContextAccessor();
            services.AddAutoMapper(cfg =>
            {
                cfg.AllowNullCollections = true;
            }, typeof(Startup).Assembly);

            services.AddMemoryCache();
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddTransient<IAccountService, AccountService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<ISettingService, SettingService>();
            services.AddScoped<IInventorySettings, InventorySettingsProvider>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<ILogService, LogService>();
            services.Configure<S3StorageOptions>(Configuration.GetSection(S3StorageOptions.SectionName));
            services.AddTransient<IStorageService, S3StorageService>();
            services.AddTransient<IProductService, ProductService>();
            services.AddTransient<IProductCategoryService, ProductCategoryService>();
            services.AddTransient<IInventoryService, InventoryService>();
            services.AddTransient<IInventoryMovementService, InventoryMovementService>();
            services.AddTransient<ISupplierService, SupplierService>();
            services.AddTransient<IRoleService, RoleService>();
            services.AddTransient<IOrganizationService, OrganizationService>();
            services.AddTransient<ILocationService, LocationService>();
            services.AddTransient<IContactService, ContactService>();
            services.AddTransient<ILeadService, LeadService>();
            services.AddTransient<IDashboardStatsService, DashboardStatsService>();

            services.AddScoped<ICurrentUserContextAccessor, CurrentUserContextAccessor>();
            services.AddScoped<ICurrentUserContextProvider, CurrentUserContextProvider>();
            services.AddScoped<IAuthorizationDomainService, AuthorizationDomainService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider services)
        {
            app.UseDetection();
            app.UseCors();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Core V1");
            });

            #region Localization

            IList<CultureInfo> supportedCultures = new List<CultureInfo>
            {
                new CultureInfo("es-ES"),
                new CultureInfo("en-US")
            };
            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(culture: "es-ES", uiCulture: "es-ES"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            };
            app.UseRequestLocalization(localizationOptions);

            var requestProvider = new RouteDataRequestCultureProvider();
            localizationOptions.RequestCultureProviders.Insert(0, requestProvider);
            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);

            #endregion Localization

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseMiddleware<CurrentUserContextMiddleware>();
            app.UseAuthorization();

            app.UseMiddleware(typeof(ErrorWrappingMiddleware));
            app.UseResponseCompression();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    ResultStatusCodes =
                    {
                        [HealthStatus.Healthy] = StatusCodes.Status200OK,
                        [HealthStatus.Degraded] = StatusCodes.Status200OK,
                        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                    }
                });

                endpoints.MapControllers();
            });

           //DatabaseSeed.SeedDatabaseAsync(services).Wait();
        }
    }
}