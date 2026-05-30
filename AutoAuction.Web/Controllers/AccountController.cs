using AutoAuction.Infrastructure.Identity;
using AutoAuction.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoAuction.Infrastructure.Data;
using AutoAuction.Domain.Entities;

namespace AutoAuction.Web.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db) : Controller
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyAccount()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(await BuildAccountProfileViewModelAsync(user));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] ProfileDetailsViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            var viewModel = await BuildAccountProfileViewModelAsync(user);
            viewModel.Profile = model;
            return View(nameof(MyAccount), viewModel);
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.PhoneNumber = model.PhoneNumber.Trim();
        user.CompanyAddress = model.CompanyAddress.Trim();

        var dealerProfile = await db.DealerProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (dealerProfile is null)
        {
            db.DealerProfiles.Add(new DealerProfile
            {
                UserId = user.Id,
                CompanyName = model.CompanyName.Trim(),
                FiscalCode = model.FiscalCode.Trim()
            });
        }
        else
        {
            dealerProfile.CompanyName = model.CompanyName.Trim();
            dealerProfile.FiscalCode = model.FiscalCode.Trim();
        }

        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await db.SaveChangesAsync();
        }

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Datele contului au fost actualizate." : "Datele contului nu au putut fi actualizate.";

        return RedirectToAction(nameof(MyAccount));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            var viewModel = await BuildAccountProfileViewModelAsync(user);
            viewModel.ChangePassword = model;
            return View(nameof(MyAccount), viewModel);
        }

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var viewModel = await BuildAccountProfileViewModelAsync(user);
            viewModel.ChangePassword = model;
            return View(nameof(MyAccount), viewModel);
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Parola a fost schimbata.";
        return RedirectToAction(nameof(MyAccount));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePasswordRecovery()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, token }, Request.Scheme);
        TempData["RecoveryLink"] = resetUrl;
        TempData["SuccessMessage"] = "Linkul de recuperare a fost generat pentru testare.";

        return RedirectToAction(nameof(MyAccount));
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Contul nu a fost gasit.");
            return View(model);
        }

        var result = await userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "Parola a fost resetata. Te poti autentifica folosind noua parola.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Register(string? role)
    {
        var selectedRole = role == AppRoles.Seller ? AppRoles.Seller : AppRoles.Buyer;
        return View(new RegisterViewModel { Role = selectedRole });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber,
            CompanyAddress = model.CompanyAddress
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            var role = model.Role == AppRoles.Seller ? AppRoles.Seller : AppRoles.Buyer;
            await userManager.AddToRoleAsync(user, role);
            db.DealerProfiles.Add(new DealerProfile
            {
                UserId = user.Id,
                CompanyName = model.CompanyName.Trim(),
                FiscalCode = model.FiscalCode.Trim()
            });
            await db.SaveChangesAsync();
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet]
    public IActionResult LoginForm()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError(string.Empty, "Email sau parola invalida.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<AccountProfileViewModel> BuildAccountProfileViewModelAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var dealerProfile = await db.DealerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        return new AccountProfileViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Role = string.Join(", ", roles),
            IsSeller = roles.Contains(AppRoles.Seller),
            CreatedAt = user.CreatedAt,
            RatingAverage = user.RatingAverage,
            RatingCount = user.RatingCount,
            CompanyName = dealerProfile?.CompanyName ?? string.Empty,
            FiscalCode = dealerProfile?.FiscalCode ?? string.Empty,
            Profile = new ProfileDetailsViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                CompanyName = dealerProfile?.CompanyName ?? string.Empty,
                FiscalCode = dealerProfile?.FiscalCode ?? string.Empty,
                CompanyAddress = user.CompanyAddress
            }
        };
    }
}
