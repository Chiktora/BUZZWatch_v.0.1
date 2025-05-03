using BuzzWatch.Web.Services;
using BuzzWatch.Web.Hubs;

namespace BuzzWatch.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            
            // Add session services
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add authentication configuration
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "Cookies";
            })
            .AddCookie("Cookies", options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            // Add HttpContextAccessor for JWT handling
            builder.Services.AddHttpContextAccessor();
            
            // Add ApiClient with JWT handler
            builder.Services.AddTransient<JwtDelegatingHandler>();
            builder.Services.AddHttpClient<ApiClient>(client =>
            {
                // Read from appsettings.json
                var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5189";
                client.BaseAddress = new Uri(apiBaseUrl);
            })
            .AddHttpMessageHandler<JwtDelegatingHandler>();
            
            // Register audit logging service
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();
            
            // Register predictive analytics service
            builder.Services.AddScoped<IPredictiveAnalyticsService, PredictiveAnalyticsService>();
            
            // Register SignalR connection
            builder.Services.AddSignalR();
            
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Use session before authentication
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            
            // Map SignalR hub
            app.MapHub<MeasurementNotificationHub>("/hubs/measurements");
            
            app.Run();
        }
    }
}
