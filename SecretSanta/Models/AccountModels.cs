using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MimeKit;
using SecretSanta.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace SecretSanta.Models
{
    public class Account
    {
        #region Variables

        public Guid? Id { get; set; }

        [DisplayName("E-Mail Address"), Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [DisplayName("Display Name"), Required]
        public string DisplayName { get; set; }

        [DisplayName("Do Not Pick")]
        public IList<Guid> DoNotPick { get; set; }

        public IDictionary<int, Guid?> Picked { get; set; }

        [DisplayName("Wish List")]
        public IDictionary<int, IList<WishlistItem>> Wishlist { get; set; }

        [DisplayName("Gift Received")]
        public IDictionary<int, ReceivedGift> ReceivedGift { get; set; }

        #endregion

        #region Public Methods

        public Account()
        {
            DoNotPick = new List<Guid>();
            Picked = new Dictionary<int, Guid?>();
            Wishlist = new Dictionary<int, IList<WishlistItem>>();
            ReceivedGift = new Dictionary<int, ReceivedGift>();
        }

        public bool HasPicked()
        {
            return GetPicked() != null;
        }

        public bool HasBeenPicked()
        {
            return GetPickedBy() != null;
        }

        public Account GetPicked()
        {
            if (Picked != null && Picked.ContainsKey(DateHelper.Year))
            {
                return DataRepository.Get<Account>(Picked[DateHelper.Year]);
            }

            return null;
        }

        public Account GetPickedBy()
        {
            var pickedBy = DataRepository.GetAll<Account>().FirstOrDefault(x =>
                x.Picked != null && x.Picked.Any(y =>
                    y.Key == DateHelper.Year &&
                    y.Value == Id
                )
            );

            return pickedBy;
        }

        public void Pick()
        {
            if (!Id.HasValue || HasPicked())
            {
                return;
            }

            var candidates = DataRepository.GetAll<Account>()
                .Where(a =>
                    a.Id != Id
                    && a.HasBeenPicked() == false
                    && !DoNotPick.Contains(a.Id.Value)
                    && !a.DoNotPick.Contains(Id.Value)
                );

            // Special-case it when only 2 options are left, and choosing
            // one of these options means that there'd be dangling candidate
            // that nobody chose.
            //
            // Example 1:
            //   Item     Receive   Give
            //      x          no     no
            //      y          no    yes
            // In this case, choosing 'y' will leave 'x' dangling.
            //
            // Example 2:
            //   Item     Receive   Give
            //      x          no    yes
            //      y          no    yes
            // In this case there's no possibility of a dangling candidate.
            //
            // Example 3:
            //   Item     Receive   Give
            //      x          no     no
            //      y          no     no
            // In this case there's no possibility of a dangling candidate.
            if (candidates.Count() == 2 && !candidates.All(a => a.HasPicked()))
            {
                candidates = candidates.Where(a => !a.HasPicked());
            }

            if (candidates.Count() > 1)
            {
                // if there's more than one potential candidate,
                // make sure not to pick the same person as the previous year
                candidates = candidates.Where(a =>
                    !Picked.Any(y => y.Key == (DateHelper.Year - 1) && y.Value == a.Id)
                );
            }

            int rand = new Random().Next(0, candidates.Count());

            if (!Picked.ContainsKey(DateHelper.Year))
            {
                Picked.Add(DateHelper.Year, null);
            }

            Picked[DateHelper.Year] = candidates.ElementAt(rand).Id;
            DataRepository.Save(this);
        }

        #endregion
    }

    public class LogInModel
    {
        public static string TokenSignIn(HttpContext httpContext, string token, string returnUrl)
        {
            var uid = GuidEncoder.Decode(token);
            var account = DataRepository.Get<Account>(uid);

            if (account != null)
            {
                var principal = new ClaimsPrincipal(new Identity(account.Id.ToString()));
                httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                returnUrl = string.IsNullOrWhiteSpace(returnUrl)
                    ? AppSettings.LoginPath.ToString()
                    : returnUrl;
            }

            return string.IsNullOrWhiteSpace(returnUrl)
                ? AppSettings.LoginPath.ToString()
                : returnUrl;
        }

        protected class Identity : IIdentity
        {
            public string AuthenticationType => CookieAuthenticationDefaults.AuthenticationScheme;

            public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Name);

            public string Name { get; private set; }

            public Identity()
            {

            }

            public Identity(string name)
            {
                Name = name;
            }
        }
    }

    public class LogOutModel
    {
        #region Public Methods

        public static string SignOut(HttpContext httpContext)
        {
            httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            httpContext.Session = null;

            return AppSettings.LoginPath;
        }

        #endregion
    }

    public class SendLogInLinkModel
    {
        #region Variables

        [Required, DisplayName("Imię")]
        public string DisplayName { get; set; }

        [Required, EmailAddress, DisplayName("Adres E-Mail")]
        public string Email { get; set; }

        #endregion

        #region Public Methods

        public void Send(IUrlHelper urlHelper)
        {
            var account = DataRepository.GetAll<Account>().FirstOrDefault(
                x => x.DisplayName.Equals(DisplayName, StringComparison.CurrentCultureIgnoreCase) && x.Email.Equals(Email, StringComparison.CurrentCultureIgnoreCase)
            );

            if (account == null/* && Email.Equals(AppSettings.AdminEmail)*/)
            {
                account = new Account
                {
                    Email = Email,
                    Id = Guid.NewGuid(),
                    DisplayName = DisplayName
                };
                DataRepository.Save(account);
            }

            if (account != null && account.Id.HasValue)
            {
                var token = GuidEncoder.Encode(account.Id.Value);
                var url = urlHelper.Action("LogIn", "Account", new { token }, "http");

                StringBuilder body = new StringBuilder()
                    .AppendLine($"Hej {account.DisplayName}!")
                    .AppendLine()
                    .Append("Z tej strony Secret Santa. Przesyłam link, dzięki któremu możesz zalogować się na moją stronę.").AppendLine()
                    .AppendLine()
                    .Append("Po prostu w niego kliknij: ")
                    .AppendLine()
                    .AppendLine($"<a href=\"{url}\">Secret Santa - Wejście ({account.DisplayName})</a>").AppendLine()
                    .AppendLine()
                    .AppendLine("Na mojej stronie będziesz mógł/mogła wybrać osobę, którą zechcesz obdarować.")
                    .AppendLine("Możesz również dodać podpowiedzi do swojego wymarzonego upominku. Może to bardzo pomóc osobie, która wybierze Ciebie do obdarowania!")
                    .AppendLine()
                    .AppendLine("Udanej zabawy! Ho ho ho, ")
                    .AppendLine()
                    .AppendLine("Santa")
                    .AppendLine();

                var to = new List<MailboxAddress> { new MailboxAddress(account.DisplayName, account.Email) };

                EmailMessage.Send(to, "Secret Santa - link do logowania", body.ToString());
            }
        }

        #endregion
    }

    public class EditUsersModel
    {
        #region Variables

        public AddUserModel NewUser { get; set; }

        public IList<EditUserModel> Users { get; set; }

        public bool AllPicked => Users.All(u =>
            u.Picked.HasValue &&
            !string.IsNullOrWhiteSpace(u.PickedBy)
        );

        #endregion

        #region Public Methods

        public EditUsersModel()
        {
            NewUser = new AddUserModel();
            Users = new List<EditUserModel>();

            foreach (var id in DataRepository.GetAll<Account>().Select(x => x.Id.Value))
            {
                Users.Add(new EditUserModel(id));
            }
        }

        public static void SendInvitationMessages(IUrlHelper urlHelper)
        {
            IList<Account> accounts = DataRepository.GetAll<Account>();

            foreach (Account account in accounts)
            {
                var token = GuidEncoder.Encode(account.Id.Value);
                string url = urlHelper.Action("LogIn", "Account", new { token }, "http");

                StringBuilder body = new StringBuilder()
                    .AppendLine($"Hej {account.DisplayName}!")
                    .AppendLine()
                    .Append("Z tej strony Secret Santa. Przesyłam link, dzięki któremu możesz zalogować się na moją stronę.")
                    .AppendLine()
                    .Append("Po prostu w niego kliknij: ")
                    .AppendLine()
                    .AppendLine($"<a href=\"{url}\">Secret Santa - Wejście ({account.DisplayName})</a>").AppendLine()
                    .AppendLine()
                    .AppendLine("Na mojej stronie będziesz mógł/mogła wybrać osobę, którą zechcesz obdarować.")
                    .AppendLine("Możesz również dodać podpowiedzi do swojego wymarzonego upominku. Może to bardzo pomóc osobie, która wybierze Ciebie do obdarowania!")
                    .AppendLine()
                    .AppendLine("Udanej zabawy! Ho ho ho, ")
                    .AppendLine()
                    .AppendLine("Santa")
                    .AppendLine();

                var to = new List<MailboxAddress> { new MailboxAddress(account.DisplayName, account.Email) };

                EmailMessage.Send(to, "Secret Santa - link do logowania", body.ToString());
            }
        }

        public static void SendAllPickedMessages(IUrlHelper urlHelper)
        {
            IList<Account> accounts = DataRepository.GetAll<Account>();

            foreach (Account account in accounts)
            {
                var token = GuidEncoder.Encode(account.Id.Value);
                string url = urlHelper.Action("LogIn", "Account", new { token }, "http");
                Account recipient = account.GetPicked();

                StringBuilder body = new StringBuilder()
                    .AppendLine($"Hey {account.DisplayName}!")
                    .AppendLine()
                    .AppendFormat("Santa here. Just wanted to let you know that everyone ")
                    .AppendLine("has now picked a person using the Secret Santa website.")
                    .AppendLine()
                    .AppendLine($"Thought I'd send a frindly reminder that you picked {recipient.DisplayName}!")
                    .AppendLine()
                    .AppendLine()
                    .AppendLine("Here's their wish list as it stands right now:")
                    .AppendLine();

                if (recipient.Wishlist.ContainsKey(DateHelper.Year))
                {
                    foreach (WishlistItem item in recipient.Wishlist[DateHelper.Year])
                    {
                        body.AppendLine($"Item: {item.Name}")
                            .AppendLine($"Description: {item.Description}")
                            .AppendLine($"Link: {item.Url}")
                            .AppendLine();
                    }
                }

                body.Append("Remember that you can always visit the address below ")
                    .AppendLine($"to update your wish list and view any changes made by {recipient.DisplayName} too!")
                    .AppendLine()
                    .AppendLine($"<a href=\"{url}\">Secret Santa Website</a>")
                    .AppendLine()
                    .AppendLine("Ho ho ho,")
                    .AppendLine()
                    .AppendLine("Santa");

                var to = new List<MailboxAddress> { new MailboxAddress(account.DisplayName, account.Email) };

                EmailMessage.Send(to, "Secret Santa Reminder", body.ToString());
            }
        }

        public static void Reset()
        {
            var users = DataRepository.GetAll<Account>();

            foreach (var user in users)
            {
                user.Picked.Remove(DateHelper.Year);
                user.ReceivedGift.Remove(DateHelper.Year);
                user.Wishlist.Remove(DateHelper.Year);

                DataRepository.Save(user);
            }
        }

        #endregion
    }

    public class AddUserModel
    {
        #region Variables

        [DisplayName("E-Mail Address"), Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [DisplayName("Display Name"), Required]
        public string DisplayName { get; set; }

        #endregion

        #region Public Methods

        public Account CreateAccount()
        {
            var account = new Account
            {
                Email = Email,
                DisplayName = DisplayName
            };

            DataRepository.Save(account);
            return account;
        }

        public bool EmailConflict()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                return false;
            }

            IList<Account> accounts = DataRepository.GetAll<Account>();

            return accounts.Any(a => a.Email.Equals(Email, StringComparison.CurrentCultureIgnoreCase));
        }

        #endregion
    }

    public class EditUserModel
    {
        #region Variables

        public Guid AccountId { get; set; }

        [Required, EmailAddress, DisplayName("E-Mail Address")]
        public string Email { get; set; }

        [Required, DisplayName("Display Name")]
        public string DisplayName { get; set; }

        public Guid? Picked { get; set; }

        [DisplayName("PickedBy")]
        public string PickedBy { get; set; }

        [DisplayName("Do Not Pick")]
        public IList<Guid> DoNotPick { get; set; }

        #endregion

        #region Public Methods

        public EditUserModel()
        {
            DoNotPick = new List<Guid>();
        }

        public EditUserModel(Guid id)
        {
            var accounts = DataRepository.GetAll<Account>();
            var account = accounts.Single(x => x.Id.Equals(id));
            var pickedBy = accounts.SingleOrDefault(x =>
                x.Picked.ContainsKey(DateHelper.Year) &&
                x.Picked[DateHelper.Year].Equals(id)
            );

            AccountId = account.Id.Value;
            Email = account.Email;
            DisplayName = account.DisplayName;
            Picked = (account.Picked?.ContainsKey(DateHelper.Year) ?? false)
                ? account.Picked[DateHelper.Year]
                : null;
            PickedBy = pickedBy?.DisplayName;
            DoNotPick = account.DoNotPick ?? new List<Guid>();
        }

        public void Save()
        {
            var account = DataRepository.Get<Account>(AccountId);

            account.Email = Email;
            account.DisplayName = DisplayName;
            account.Picked[DateHelper.Year] = Picked;
            account.DoNotPick = DoNotPick;

            DataRepository.Save(account);
        }

        public void Delete()
        {
            DataRepository.Delete<Account>(AccountId);
        }

        public IEnumerable<SelectListItem> GetPickOptions()
        {
            return DataRepository.GetAll<Account>()
                .Where(a => !a.Id.Equals(AccountId) && !DoNotPick.Contains(a.Id.Value))
                .Select(a => new SelectListItem
                {
                    Text = a.DisplayName,
                    Value = a.Id.ToString(),
                    Selected = Picked.Equals(a.Id)
                });
        }

        public IEnumerable<SelectListItem> GetDoNotPickOptions()
        {
            return DataRepository.GetAll<Account>()
                .Where(a => a.Id.HasValue && !a.Email.Equals(Email, StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(a => a.DisplayName)
                .Select(a => new SelectListItem
                {
                    Text = a.DisplayName,
                    Value = a.Id.ToString(),
                    Selected = DoNotPick.Any(b => b.Equals(a.Id))
                });
        }

        #endregion
    }

    public class PickModel
    {
        #region Variables

        public Account Account { get; set; }

        public IList<Account> Candidates { get; set; }

        #endregion

        #region Public Methods

        public PickModel(Guid id)
        {
            Candidates = new List<Account>();

            if (id.Equals(Guid.Empty))
            {
                return;
            }

            var accounts = DataRepository.GetAll<Account>();
            Account = accounts.Single(x => x.Id.Equals(id));

            var candidates = DataRepository.GetAll<Account>()
                .Where(a =>
                    a.Id != Account.Id
                    && a.HasBeenPicked() == false
                    && !Account.DoNotPick.Contains(a.Id.Value)
                    && !a.DoNotPick.Contains(Account.Id.Value)
                );
            foreach (var user in candidates)
            {
                Candidates.Add(user);
            }
        }

        public void Pick(Guid id)
        {
            var accounts = DataRepository.GetAll<Account>();
            var candidateAccount = accounts.Single(x => x.Id.Equals(id));
            if (candidateAccount.HasBeenPicked())
            {
                // already picked
                return;
            }

            Account.Picked[DateHelper.Year] = candidateAccount.Id;
            DataRepository.Save(Account);
        }


        #endregion
    }
}
