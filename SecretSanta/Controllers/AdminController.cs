﻿using System.Web.Mvc;
using SecretSanta.Models;
using SecretSanta.Utilities;

namespace SecretSanta.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        //
        // GET: /Admin/Users
        public ActionResult Users()
        {
            EditUsersModel model = EditUsersModel.Load();
            return View(model);
        }

        //
        // GET: /Invite
        public ActionResult Invite()
        {
            var urlHelper = new UrlHelper(ControllerContext.RequestContext);
            EditUsersModel.SendInvitationMessages(urlHelper);
            this.SetResultMessage("Invitations successfully sent to all users.");
            return RedirectToAction("Users");
        }

        //
        // GET: /AllPicked
        public ActionResult AllPicked()
        {
            var urlHelper = new UrlHelper(ControllerContext.RequestContext);
            EditUsersModel.SendAllPickedMessages(urlHelper);
            this.SetResultMessage("Reminders successfully sent to all users.");
            return RedirectToAction("Users");
        }

        //
        // POST: /Admin/AddUser
        [HttpPost]
        public ActionResult AddUser(AddUserModel model)
        {
            if (model.EmailConflict())
            {
                ModelState.AddModelError("NewUser.Email", "A user already exists with the specified E-Mail Address.");
            }

            if (ModelState.IsValid)
            {
                model.CreateAccount();
                this.SetResultMessage(string.Format("<strong>Successfully added</strong> {0}.", model.DisplayName));
            }

            return RedirectToAction("Users");
        }

        //
        // POST: /Admin/EditUser
        [HttpPost]
        public ActionResult EditUser(EditUserModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                this.SetResultMessage(string.Format("<strong>Successfully updated</strong> {0}.", model.Account.DisplayName));
            }

            return RedirectToAction("Users");
        }

        //
        // POST: /Admin/DeleteUser
        [HttpPost]
        public ActionResult DeleteUser(EditUserModel model)
        {
            model.Delete();
            this.SetResultMessage(string.Format("<strong>Successfully deleted</strong> {0}.", model.Account.DisplayName));

            return RedirectToAction("Users");
        }
    }
}