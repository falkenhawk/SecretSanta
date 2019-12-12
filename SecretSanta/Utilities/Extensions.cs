using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SecretSanta.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace SecretSanta.Utilities
{
    public static class Extensions
    {
        public static Account GetAccount(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated || string.IsNullOrWhiteSpace(user.Identity.Name))
            {
                return null;
            }

            Account account = DataRepository.GetAll<Account>().SingleOrDefault(a =>
                user.Identity.Name.Equals((a.Id ?? Guid.Empty).ToString(), StringComparison.CurrentCultureIgnoreCase)
            );

            if (account == null)
            {
                throw new UnauthorizedAccessException("Access denied.");
                // var model = new AddUserModel
                // {
                //     DisplayName = user.Identity.Name,
                //     Email = user.Identity.Name
                // };
                // account = model.CreateAccount();
            }

            return account;
        }

        public static void SetResultMessage(this Controller controller, string message)
        {
            controller.HttpContext.Session.Set("ResultMessage", Encoding.UTF8.GetBytes(message));
        }

        public static IHtmlContent ResultMessage(this IHtmlHelper helper)
        {
            helper.ViewContext.HttpContext.Session.TryGetValue("ResultMessage", out byte[] messageBytes);
            string output = string.Empty;

            if (messageBytes != null && messageBytes.Length > 0)
            {
                var message = Encoding.UTF8.GetString(messageBytes);

                if (!string.IsNullOrWhiteSpace(message))
                {
                    output = $@"
<div class='alert alert-info alert-dismissable'>
    <button type='button' class='close' data-dismiss='alert' aria-hidden='true'>&times;</button>
    {message}
</div>
";
                }

                helper.ViewContext.HttpContext.Session.Remove("ResultMessage");
            }

            return new HtmlString(output);
        }
    }
}
