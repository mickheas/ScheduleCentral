using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScheduleCentral.Models.ViewModels;
using ScheduleCentral.Services;
using System.Security.Claims;

namespace ScheduleCentral.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly NotificationService _notifications;

        public NotificationsController(NotificationService notifications)
        {
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = new NotificationsIndexViewModel
            {
                UnreadCount = await _notifications.GetUnreadCountAsync(userId!),
                Notifications = await _notifications.GetRecentAsync(userId!, take: 50)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id, string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _notifications.MarkReadAsync(userId!, id);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                var safe = ToSafeLocalUrl(returnUrl);
                if (safe != null) return LocalRedirect(safe);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead(string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _notifications.MarkAllReadAsync(userId!);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                var safe = ToSafeLocalUrl(returnUrl);
                if (safe != null) return LocalRedirect(safe);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Go(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notif = await _notifications.GetByIdAsync(userId!, id);
            await _notifications.MarkReadAsync(userId!, id);

            if (notif != null && !string.IsNullOrWhiteSpace(notif.Url))
            {
                var safe = ToSafeLocalUrl(notif.Url);
                if (safe != null) return LocalRedirect(safe);
            }

            return RedirectToAction("Index", "Home");
        }

        private string? ToSafeLocalUrl(string url)
        {
            url = (url ?? "").Trim();
            if (string.IsNullOrWhiteSpace(url)) return null;

            if (Url.IsLocalUrl(url)) return url;

            if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            {
                var requestHost = Request.Host.Host;
                var requestPort = Request.Host.Port;

                if (string.Equals(absolute.Host, requestHost, StringComparison.OrdinalIgnoreCase)
                    && (!requestPort.HasValue || absolute.Port == requestPort.Value))
                {
                    var local = absolute.PathAndQuery + absolute.Fragment;
                    if (Url.IsLocalUrl(local)) return local;
                }
            }

            return null;
        }
    }
}
