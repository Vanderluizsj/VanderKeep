using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VKeeper.Infraestrutura.Identity;
using VKeeper.Web.Servicos;
using VKeeper.Web.ViewModels.Account;

namespace VKeeper.Web.Controllers;

public sealed class AccountController : Controller
{
    private const string InvalidLoginMessage = "E-mail ou senha inválidos.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new RegisterViewModel
        {
            ReturnUrl = SanitizeReturnUrl(returnUrl)
        });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        model.ReturnUrl = SanitizePostedReturnUrl(model.ReturnUrl);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim();
        var displayName = model.DisplayName.Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            ModelState.AddModelError(nameof(model.DisplayName), "O nome é obrigatório.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            DisplayName = displayName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var errorMessage in IdentityErrorTranslator.Translate(result.Errors))
            {
                ModelState.AddModelError(string.Empty, errorMessage);
            }

            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("New user account registered and signed in. UserId: {UserId}", user.Id);

        TempData["SuccessMessage"] = "Conta criada com sucesso! Bem-vindo ao VKeeper.";
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel
        {
            ReturnUrl = SanitizeReturnUrl(returnUrl)
        });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.ReturnUrl = SanitizePostedReturnUrl(model.ReturnUrl);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _logger.LogWarning("Failed login attempt for unregistered account.");
            ModelState.AddModelError(string.Empty, InvalidLoginMessage);
            return View(model);
        }

        var signInResult = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (signInResult.Succeeded)
        {
            _logger.LogInformation("User signed in successfully. UserId: {UserId}", user.Id);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (signInResult.IsLockedOut)
        {
            _logger.LogWarning("User account locked out after repeated failed login attempts. UserId: {UserId}", user.Id);
            ModelState.AddModelError(
                string.Empty,
                "Conta temporariamente bloqueada por excesso de tentativas inválidas. Tente novamente em alguns minutos.");
            return View(model);
        }

        _logger.LogWarning("Failed login attempt due to invalid credentials. UserId: {UserId}", user.Id);
        ModelState.AddModelError(string.Empty, InvalidLoginMessage);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _signInManager.SignOutAsync();

        _logger.LogInformation("User signed out. UserId: {UserId}", userId);
        TempData["InfoMessage"] = "Você saiu da sua conta com segurança.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed password change attempt. UserId: {UserId}", user.Id);
            foreach (var errorMessage in IdentityErrorTranslator.Translate(result.Errors))
            {
                ModelState.AddModelError(string.Empty, errorMessage);
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("User changed password successfully. UserId: {UserId}", user.Id);

        TempData["SuccessMessage"] = "Senha alterada com sucesso.";
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return RedirectToAction("HttpStatusCodeHandler", "Error", new { statusCode = StatusCodes.Status403Forbidden });
    }

    private string? SanitizePostedReturnUrl(string? returnUrl)
    {
        var sanitized = SanitizeReturnUrl(returnUrl);
        if (sanitized is null && !string.IsNullOrEmpty(returnUrl))
        {
            ModelState.Remove(nameof(LoginViewModel.ReturnUrl));
        }

        return sanitized;
    }

    private string? SanitizeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        var safeUrl = SanitizeReturnUrl(returnUrl);
        if (safeUrl is not null)
        {
            return LocalRedirect(safeUrl);
        }

        return RedirectToAction("Index", "Notes");
    }
}
