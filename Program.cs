using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using MyfirstApp.Services;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

// ✅ Initialize Firebase
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("firebase-config.json") // Ensure this file exists
});

// ✅ Add MVC Services
builder.Services.AddControllersWithViews();

// ✅ Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// ✅ Add Authentication Services
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "ORCID"; // Default to ORCID authentication
})
.AddCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax; // Ensure cookies work across domains
})
.AddOAuth("ORCID", options =>
{
    var config = builder.Configuration.GetSection("Authentication:ORCID");
    options.ClientId = config["ClientId"];
    options.ClientSecret = config["ClientSecret"];

    // ✅ Corrected Callback Path
    options.CallbackPath = new PathString("/signin-orcid");  // This should match your ORCID app settings

    // ✅ ORCID Endpoints
    options.AuthorizationEndpoint = "https://orcid.org/oauth/authorize";
    options.TokenEndpoint = "https://orcid.org/oauth/token";
    options.UserInformationEndpoint = "https://pub.orcid.org/v3.0/";

    options.SaveTokens = true;

    // ✅ Ensure scope parameter is included
    options.Scope.Clear();
    var scopes = config["Scope"];
    if (!string.IsNullOrEmpty(scopes))
    {
        foreach (var scope in scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            options.Scope.Add(scope);
        }
    }

    // ✅ Map ORCID user info to claims
    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "orcid");
    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");

    options.Events.OnCreatingTicket = async context =>
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{options.UserInformationEndpoint}{context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

        var response = await context.Backchannel.SendAsync(request);
        response.EnsureSuccessStatusCode(); // ✅ Ensure no request failures

        var json = await response.Content.ReadAsStringAsync();
        var user = JsonDocument.Parse(json).RootElement;
        context.RunClaimActions(user);
    };
});

var app = builder.Build();

// ✅ Ensure Firebase & Database Don't Break Views
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ✅ Ensure Cookies Work for OAuth
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax
});

// ✅ Enable Authentication & Authorization Middleware
app.UseAuthentication();
app.UseAuthorization();

// ✅ Map Routes Correctly
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
