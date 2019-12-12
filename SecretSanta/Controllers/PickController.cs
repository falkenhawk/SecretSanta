using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretSanta.Models;
using SecretSanta.Utilities;
using System;

namespace SecretSanta.Controllers
{
    [Authorize]
    public class PickController : Controller
    {
        //
        // GET: /Pick/
        public IActionResult Index()
        {
            Account account = User.GetAccount();
            var model = new PickModel(account.Id ?? Guid.Empty);
            return View(model);
        }

        //
        // GET: /Pick/Pick/:id
        public IActionResult Pick(Guid id)
        {
            Account account = User.GetAccount();
            var model = new PickModel(account.Id ?? Guid.Empty);
            model.Pick(id);

            return RedirectToAction("Index", "Home");
        }
    }
}
