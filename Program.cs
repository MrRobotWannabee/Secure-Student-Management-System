using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Secure_Student_Management_System.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Secure_Student_Management_System.Controllers;
using Secure_Student_Management_System.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity with Role Management
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure Authentication (Google & OpenID Connect)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = "https://login.microsoftonline.com/your-tenant-id-here";
    options.ClientId = builder.Configuration["Authentication:OIDC:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:OIDC:ClientSecret"] ?? "";
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.ClaimActions.MapJsonKey("role", "role");
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("User", policy => policy.RequireRole("User"));
});

// Configure Azure Cosmos DB
builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    string cosmosConnectionString = builder.Configuration.GetConnectionString("AzureCosmosDB")
        ?? throw new InvalidOperationException("Cosmos DB connection string not found.");
    return new CosmosClient(cosmosConnectionString);
});

// Configure Azure Blob Storage
builder.Services.AddSingleton<BlobServiceClient>(serviceProvider =>
{
    string blobConnectionString = builder.Configuration["AzureStorage:ConnectionString"]
        ?? throw new InvalidOperationException("Azure Blob Storage connection string not found.");
    return new BlobServiceClient(blobConnectionString);
});

// Add MVC & Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Register ImageUploadService and StudentService (FIXED)
// ✅ Correct service registration with all dependencies
builder.Services.AddScoped<ImageUploadService>(provider =>
{
    return new ImageUploadService(
        provider.GetRequiredService<BlobServiceClient>(),
        provider.GetRequiredService<IConfiguration>(),
        provider.GetRequiredService<ILogger<ImageUploadService>>() // Add logger
    );
});

builder.Services.AddScoped<StudentService>();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Middleware setup
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Seed Admin User (corrected to use ApplicationUser)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); // ✅

    // Create roles
    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Create admin user
    var adminEmail = "admin@example.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser // ✅ Correct user type
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User"
        };
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

app.Run();