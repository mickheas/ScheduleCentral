using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;

namespace ScheduleCentral.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task CreateAsync(string userId, string title, string? message = null, string? url = null, string? category = null)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            _context.UserNotifications.Add(new UserNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Url = url,
                Category = category,
                CreatedAtUtc = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();
        }

        public async Task CreateForUsersAsync(IEnumerable<string> userIds, string title, string? message = null, string? url = null, string? category = null)
        {
            var ids = userIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (ids.Count == 0) return;

            foreach (var userId in ids)
            {
                _context.UserNotifications.Add(new UserNotification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Url = url,
                    Category = category,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task CreateForRolesAsync(IEnumerable<string> roles, string title, string? message = null, string? url = null, string? category = null)
        {
            var roleList = roles?.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                           ?? new List<string>();
            if (roleList.Count == 0) return;

            var users = new List<ApplicationUser>();
            foreach (var role in roleList)
            {
                var roleUsers = await _userManager.GetUsersInRoleAsync(role);
                users.AddRange(roleUsers);
            }

            var distinctIds = users.Select(u => u.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (distinctIds.Count == 0) return;

            foreach (var userId in distinctIds)
            {
                _context.UserNotifications.Add(new UserNotification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Url = url,
                    Category = category,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return 0;

            return await _context.UserNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<List<UserNotification>> GetRecentAsync(string userId, int take = 10)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<UserNotification>();

            take = Math.Clamp(take, 1, 50);

            return await _context.UserNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public async Task<UserNotification?> GetByIdAsync(string userId, int notificationId)
        {
            if (string.IsNullOrWhiteSpace(userId) || notificationId <= 0) return null;

            return await _context.UserNotifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        }

        public async Task MarkReadAsync(string userId, int notificationId)
        {
            if (string.IsNullOrWhiteSpace(userId) || notificationId <= 0) return;

            var notif = await _context.UserNotifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif == null) return;

            if (!notif.IsRead)
            {
                notif.IsRead = true;
                notif.ReadAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllReadAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            var unread = await _context.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAtUtc = now;
            }

            await _context.SaveChangesAsync();
        }
    }
}
