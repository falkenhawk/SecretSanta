using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Newtonsoft.Json;
using SecretSanta.Utilities;
using SecretSanta.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace SecretSanta.Models
{
    public class ReceivedGift
    {
        #region Constants

        private const double IMAGE_MAX_WIDTH = 1024;
        private const double IMAGE_MAX_HEIGHT = 768;

        #endregion

        #region Variables

        [Required]
        public Guid? Id { get; set; }

        [Required]
        public string From { get; set; }

        [Required]
        public string To { get; set; }

        [DisplayName("Note"), Required]
        public string Description { get; set; }

        public byte[] Image { get; set; }

        [DisplayName("Photo with your gift"), Required, JsonIgnore, MaxFileSize(2000000)]
        public IFormFile ImageUpload { get; set; }

        #endregion

        #region Public Methods

        public void Save(Account account)
        {
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                using (var inStream = ImageUpload.OpenReadStream())
                using (var inImage = new Bitmap(inStream))
                {
                    var scaleWidth = IMAGE_MAX_WIDTH / inImage.Width;
                    var scaleHeight = IMAGE_MAX_HEIGHT / inImage.Height;
                    var scaleFactor = Math.Min(scaleWidth, scaleHeight);
                    var outRect = new Rectangle(
                        0,
                        0,
                        (int)Math.Round(scaleFactor * inImage.Width),
                        (int)Math.Floor(scaleFactor * inImage.Height)
                    );

                    using (var outImage = new Bitmap(outRect.Width, outRect.Height))
                    using (var outStream = new MemoryStream())
                    using (var gfx = Graphics.FromImage(outImage))
                    using (var imageAttributes = new ImageAttributes())
                    {
                        gfx.CompositingMode = CompositingMode.SourceCopy;
                        gfx.CompositingQuality = CompositingQuality.HighQuality;
                        gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gfx.SmoothingMode = SmoothingMode.HighQuality;
                        gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        imageAttributes.SetWrapMode(WrapMode.TileFlipXY);

                        gfx.DrawImage(inImage, outRect, 0, 0, inImage.Width, inImage.Height, GraphicsUnit.Pixel, imageAttributes);

                        outImage.Save(outStream, ImageFormat.Jpeg);
                        Image = outStream.ToArray();
                    }
                }
            }

            Id = account.Id;
            account.ReceivedGift[DateHelper.Year] = this;
            DataRepository.Save(account);
        }

        #endregion
    }

    public class ReceivedGiftEditModel
    {
        #region Variables

        public ReceivedGift Item { get; set; }

        public IEnumerable<ReceivedGift> Gifts { get; set; }

        #endregion

        #region Public Methods

        public ReceivedGiftEditModel()
        {
            Gifts = DataRepository.GetAll<Account>().Where(x =>
                        x.ReceivedGift.ContainsKey(DateHelper.Year) &&
                        x.ReceivedGift[DateHelper.Year] != null &&
                        !string.IsNullOrWhiteSpace(x.ReceivedGift[DateHelper.Year].Description)
                    )
                    .Select(x => x.ReceivedGift[DateHelper.Year])
                    .ToList();
        }

        public ReceivedGiftEditModel(Account account) : this()
        {
            Item = account.ReceivedGift.ContainsKey(DateHelper.Year)
                ? account.ReceivedGift[DateHelper.Year]
                : new ReceivedGift();
            Item.Id = account.Id;

            if (string.IsNullOrWhiteSpace(Item.From))
            {
                var pickedBy = account.GetPickedBy();
                if (pickedBy != null)
                {
                    Item.From = pickedBy.DisplayName;
                    Item.To = account.DisplayName;
                }
            }
        }

        public static void SendReminders(IUrlHelper urlHelper)
        {
            var accounts = DataRepository.GetAll<Account>().Where(x =>
                x.ReceivedGift == null ||
                !x.ReceivedGift.ContainsKey(DateHelper.Year) ||
                string.IsNullOrWhiteSpace(x.ReceivedGift[DateHelper.Year].Description)
            );

            foreach (var account in accounts)
            {
                string url = urlHelper.Action("LogIn", "Account", new { id = account.Id }, "http");
                string body = new StringBuilder()
                    .AppendFormat("Hey {0}, ", account.DisplayName).AppendLine()
                    .AppendLine()
                    .AppendFormat("Santa again. Just wanted to remind you that once you've received your gift, ")
                    .AppendFormat("you can visit the address below and add a thank-you note about the  ")
                    .AppendFormat("present you received so that everyone can see the results of the ")
                    .AppendFormat("gift exchange! ").AppendLine()
                    .AppendLine()
                    .AppendFormat("<a href=\"{0}\">Secret Santa Website</a>", url).AppendLine()
                    .AppendLine()
                    .AppendFormat("Ho ho ho, ").AppendLine()
                    .AppendLine()
                    .AppendFormat("Santa ").AppendLine()
                    .ToString();

                var to = new List<MailboxAddress> { new MailboxAddress(account.DisplayName, account.Email) };

                EmailMessage.Send(to, "Secret Santa Reminder", body);
            }
        }

        #endregion
    }
}
