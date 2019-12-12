using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretSanta.Models;
using SecretSanta.Utilities;

namespace SecretSanta.Controllers
{
    public class AccountController : Controller
    {
        //
        // GET: /Account/LogIn
        public IActionResult LogIn(string token, string returnUrl)
        {
            var redirect = Url.Action("Index", "Home");

            if (!string.IsNullOrWhiteSpace(token))
            {
                redirect = LogInModel.TokenSignIn(Request.HttpContext, token, returnUrl);
            }

            return Redirect(redirect);
        }

        //
        // GET: /Account/LogOut
        [Authorize]
        public IActionResult LogOut()
        {
            return Redirect(LogOutModel.SignOut(Request.HttpContext));
        }

        //
        // POST: /Account/SendLogInLink
        public IActionResult SendLogInLink(SendLogInLinkModel model)
        {
            if (ModelState.IsValid)
            {
                model.Send(Url);
                this.SetResultMessage($"Wiadomość została wysłana na adres <strong>{model.Email}</strong>.");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
