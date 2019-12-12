using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretSanta.Models;
using SecretSanta.Utilities;
using System;

namespace SecretSanta.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        //
        // GET: /Wishlist/
        public IActionResult Index()
        {
            var model = new WishlistEditModel(User.GetAccount().Id.Value);
            return View(model);
        }

        //
        // GET: /Wishlist/Details
        public IActionResult Details(Guid id)
        {
            var model = new WishlistEditModel(id);
            return View(model);
        }

        //
        // POST: /Wishlist/AddItem
        [HttpPost]
        public IActionResult AddItem(WishlistItem model)
        {
            if (ModelState.IsValid)
            {
                WishlistManager.AddItem(User.GetAccount(), model);
                this.SetResultMessage($"<strong>Podpowiedź dodana:</strong> {model.Name}.");

                WishlistManager.SendUpdate(User.GetAccount(), Url);
            }

            return RedirectToAction("Index");
        }

        //
        // POST: /Wishlist/EditItem
        [HttpPost]
        public IActionResult EditItem(WishlistItem model)
        {
            if (ModelState.IsValid)
            {
                WishlistManager.EditItem(User.GetAccount(), model);
                this.SetResultMessage($"<strong>Podpowiedź zmieniona:</strong> {model.Name}.");

                WishlistManager.SendUpdate(User.GetAccount(), Url);
            }

            return RedirectToAction("Index");
        }

        //
        // POST: /Wishlist/DeleteItem
        [HttpPost]
        public IActionResult DeleteItem(WishlistItem model)
        {
            if (ModelState.IsValid)
            {
                WishlistManager.DeleteItem(User.GetAccount(), model);
                this.SetResultMessage($"<strong>Podpowiedź usunięta:</strong> {model.Name}.");

                WishlistManager.SendUpdate(User.GetAccount(), Url);
            }

            return RedirectToAction("Index");
        }

        //
        // GET: /Wishlist/Remind
        public IActionResult Remind(Guid id)
        {
            WishlistManager.SendReminder(id, Url);
            this.SetResultMessage("<strong>Przypominajka została wysłana</strong>");
            // return RedirectToAction("Details", new { id });

            Account model = User.GetAccount();
            if (!model.HasPicked())
            {
                return RedirectToAction("Index", "Pick");
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
