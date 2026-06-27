using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ScheduleCentral.Services;
using System;

namespace ScheduleCentral.Controllers
{
    public class DemoController : Controller
    {
        private static readonly HashSet<string> ValidDemoRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin", "ProgramOfficer", "Instructor", "Department", "TopManagement", "Student"
        };

        [HttpGet("Demo")]
        public IActionResult Index(string role)
        {
            if (string.IsNullOrWhiteSpace(role) || !ValidDemoRoles.Contains(role))
            {
                TempData["Error"] = "Invalid demo role selected.";
                return RedirectToAction("Index", "Home");
            }

            var canonicalRole = ValidDemoRoles.First(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
            
            // Set the session role and clear seed status to trigger fresh database initialization
            HttpContext.Session.SetString("DemoRole", canonicalRole);
            HttpContext.Session.Remove("DemoDbSeeded");

            // Close existing connection if any to restart database state
            DemoDbContextConnectionCache.ClearConnection(HttpContext.Session.Id);

            TempData["Success"] = $"Demo mode initiated successfully. Welcome, {canonicalRole}!";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("Demo/SwitchRole")]
        public IActionResult SwitchRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role) || !ValidDemoRoles.Contains(role))
            {
                TempData["Error"] = "Invalid demo role selected.";
                return RedirectToAction("Index", "Home");
            }

            var canonicalRole = ValidDemoRoles.First(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
            HttpContext.Session.SetString("DemoRole", canonicalRole);

            TempData["Success"] = $"Switched demo role context to {canonicalRole}.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("Demo/Reset")]
        [ValidateAntiForgeryToken]
        public IActionResult Reset()
        {
            var activeRole = HttpContext.Session.GetString("DemoRole");
            
            HttpContext.Session.Remove("DemoDbSeeded");
            DemoDbContextConnectionCache.ClearConnection(HttpContext.Session.Id);

            TempData["Success"] = "Demo database session state reset to default seeded records.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("Demo/Exit")]
        [ValidateAntiForgeryToken]
        public IActionResult Exit()
        {
            var sessionId = HttpContext.Session.Id;
            HttpContext.Session.Remove("DemoRole");
            HttpContext.Session.Remove("DemoDbSeeded");
            DemoDbContextConnectionCache.ClearConnection(sessionId);

            TempData["Success"] = "Exited Demo mode successfully.";
            return RedirectToAction("Index", "Home");
        }
    }
}
