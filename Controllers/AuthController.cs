using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

public class AuthController : Controller
{
    // 🔹 Login with ORCID
    public IActionResult Login()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("Callback", "Auth")
        };
        return Challenge(properties, "ORCID");
    }

    // 🔹 Logout
    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    // 🔹 Callback after ORCID authentication
    public async Task<IActionResult> Callback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync("ORCID");

        if (!authenticateResult.Succeeded)
            return RedirectToAction("Login");

        // Extract user data
        var claims = authenticateResult.Principal?.Identities.FirstOrDefault()?.Claims;
        var orcidId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        if (orcidId == null)
            return RedirectToAction("Login");

        // Store ORCID data in session (or database)
        HttpContext.Session.SetString("ORCID", orcidId);
        HttpContext.Session.SetString("UserName", name ?? "Unknown");

        return RedirectToAction("Index", "Home");
    }
}
