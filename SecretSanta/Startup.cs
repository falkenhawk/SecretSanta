using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecretSanta.Utilities;
using System;

namespace SecretSanta
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            AppSettings.Initialize(Configuration);
            DataRepository.Initialize(env.ContentRootPath);
            PreviewGenerator.Initialize(env.WebRootPath);
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddDistributedMemoryCache();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.LoginPath = AppSettings.LoginPath;
                    o.LogoutPath = AppSettings.LogoutPath;
                    o.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    o.SlidingExpiration = true;
                });

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseSession();
            app.UseMvcWithDefaultRoute();

            Console.WriteLine("Settings:");
            Console.WriteLine($"AppSettings.AdminEmail: {AppSettings.AdminEmail}");
            Console.WriteLine($"AppSettings.MaxImagesToLoad: {AppSettings.MaxImagesToLoad}");
            Console.WriteLine($"AppSettings.DefaultPreviewImage: {AppSettings.DefaultPreviewImage}");
            Console.WriteLine($"AppSettings.DataDirectory: {AppSettings.DataDirectory}");
            Console.WriteLine($"AppSettings.AccountFilePattern: {AppSettings.AccountFilePattern}");
            Console.WriteLine($"AppSettings.GiftDollarLimit: {AppSettings.GiftDollarLimit}");
            Console.WriteLine($"AppSettings.SmtpHost: {AppSettings.SmtpHost}");
            Console.WriteLine($"AppSettings.SmtpPort: {AppSettings.SmtpPort}");
            Console.WriteLine($"AppSettings.SmtpUser: {AppSettings.SmtpUser}");
            Console.WriteLine($"AppSettings.SmtpPass: {AppSettings.SmtpPass}");
            Console.WriteLine($"AppSettings.SmtpFrom: {AppSettings.SmtpFrom}");
        }
    }
}
