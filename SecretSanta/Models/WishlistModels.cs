using Microsoft.AspNetCore.Mvc;
using MimeKit;
using SecretSanta.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace SecretSanta.Models
{
    public class WishlistItem
    {
        #region Variables

        public Guid? Id { get; set; }

        [Required]
        [DisplayName("Podpowiedź")]
        public string Name { get; set; }

        [DisplayName("Dodatkowy opis, szczegóły (opcjonalnie)")]
        public string Description { get; set; }

        [DisplayName("Link"), DataType(DataType.Url)]
        public string Url { get; set; }

        public byte[] PreviewImage { get; set; }

        #endregion
    }

    public class WishlistEditModel
    {
        #region Variables

        public Guid AccountId { get; set; }

        public string DisplayName { get; set; }

        public WishlistItem NewItem { get; set; }

        public IList<WishlistItem> Items { get; set; }

        #endregion

        #region Public Methods

        public WishlistEditModel()
        {
            NewItem = new WishlistItem();
            Items = new List<WishlistItem>();
        }

        public WishlistEditModel(Guid id)
        {
            var account = DataRepository.Get<Account>(id);
            AccountId = account.Id.Value;
            DisplayName = account.DisplayName;
            Items = (account?.Wishlist?.ContainsKey(DateHelper.Year) ?? false)
                ? account.Wishlist[DateHelper.Year]
                : new List<WishlistItem>();
        }

        #endregion
    }

    public static class WishlistManager
    {
        #region Public Methods

        public static async void AddItem(Account account, WishlistItem item)
        {
            if (!account.Wishlist.ContainsKey(DateHelper.Year))
            {
                account.Wishlist.Add(DateHelper.Year, new List<WishlistItem>());
            }
            item.Id = Guid.NewGuid();
            item.PreviewImage = await PreviewGenerator.GetFeaturedImage(item.Url);
            account.Wishlist[DateHelper.Year].Add(item);
            DataRepository.Save(account);
        }

        public static async void EditItem(Account account, WishlistItem item)
        {
            WishlistItem remove = account.Wishlist[DateHelper.Year].SingleOrDefault(i => i.Id.Equals(item.Id));
            account.Wishlist[DateHelper.Year].Remove(remove);
            item.PreviewImage = await PreviewGenerator.GetFeaturedImage(item.Url);
            account.Wishlist[DateHelper.Year].Add(item);
            DataRepository.Save(account);
        }

        public static void DeleteItem(Account account, WishlistItem item)
        {
            WishlistItem remove = account.Wishlist[DateHelper.Year].SingleOrDefault(i => i.Id.Equals(item.Id));
            account.Wishlist[DateHelper.Year].Remove(remove);
            DataRepository.Save(account);
        }

        public static void SendReminder(Guid id, IUrlHelper urlHelper)
        {
            var account = DataRepository.Get<Account>(id);

            var token = GuidEncoder.Encode(account.Id.Value);
            var url = urlHelper.Action("LogIn", "Account", new { token }, "http");

            string body = new StringBuilder()
                .AppendFormat("Hej {0}, ", account.DisplayName).AppendLine()
                .AppendLine()
                .AppendFormat("Z tej strony Secret Santa. Pewien ktoś prosi, abyś uzupełnił(a) swoją listę podpowiedzi dla wymarzonego prezentu.").AppendLine()
                .AppendFormat("Bardzo to pomoże tej osobie dokonać wyboru! Jeśli już uzupełniłaś(eś) swoją listę wcześniej, być może potrzebne są dokładniejsze wskazówki.").AppendLine()
                .AppendLine()
                .AppendFormat("Aby to zrobić, wejdź na moją stronę:").AppendLine()
                .AppendLine()
                .AppendLine($"<a href=\"{url}\">Secret Santa - Wejście ({account.DisplayName})</a>").AppendLine()
                .AppendLine()
                .AppendFormat("Ho ho ho, ").AppendLine()
                .AppendLine()
                .AppendFormat("Secret Santa").AppendLine()
                .ToString();

            var to = new List<MailboxAddress> { new MailboxAddress(account.DisplayName, account.Email) };

            EmailMessage.Send(to, "Secret Santa - Prośba o podpowiedzi", body);
        }

        public static void SendUpdate(Account acc, IUrlHelper urlHelper)
        {
            if (!acc.HasBeenPicked())
            {
                return;
            }

            var account = acc.GetPickedBy();

            var token = GuidEncoder.Encode(account.Id.Value);
            var url = urlHelper.Action("LogIn", "Account", new { token }, "http");

            string body = new StringBuilder()
                .AppendFormat("Hej {0}, ", account.DisplayName).AppendLine()
                .AppendLine()
                .AppendFormat("Z tej strony Secret Santa. Chcę tylko poinformować, że osoba, którą zamierzasz obdarować wprowadziła zmiany na swojej liście podpowiedzi dla wymarzonego prezentu.").AppendLine()
                .AppendLine()
                .AppendFormat("Wejdź na moją stronę, aby dowiedzieć się, co nowego się pojawiło:").AppendLine()
                .AppendLine()
                .AppendLine($"<a href=\"{url}\">Secret Santa - Wejście ({account.DisplayName})</a>").AppendLine()
                .AppendLine()
                .AppendFormat("Ho ho ho, ").AppendLine()
                .AppendLine()
                .AppendFormat("Secret Santa").AppendLine()
                .ToString();

            var to = new List<MailboxAddress> { new MailboxAddress(account.DisplayName, account.Email) };

            EmailMessage.Send(to, $"Secret Santa - Podpowiedź od: {acc.DisplayName}", body);
        }

        #endregion
    }
}
