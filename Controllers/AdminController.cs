// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Rendering;
// using Microsoft.EntityFrameworkCore;
// using ScheduleCentral.Models;
// using ScheduleCentral.Models.ViewModels;

// namespace ScheduleCentral.Controllers
// {
//     [Authorize(Roles = "Admin")]
//     public class AdminController : Controller
//     {
//         private readonly UserManager<ApplicationUser> _userManager;
//         private readonly RoleManager<IdentityRole> _roleManager;
//         private readonly ILogger<AdminController> _logger;

//         public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<AdminController> logger)
//         {
//             _userManager = userManager;
//             _roleManager = roleManager;
//             _logger = logger;
//         }

//         // GET: /Admin/
//         public IActionResult Index()
//         {
//             return View();
//         }

//         // GET: /Admin/ListUsers
//         public async Task<IActionResult> ListUsers()
//         {
//             var users = await _userManager.Users.ToListAsync();
//             return View(users);
//         }

//         // POST: /Admin/ApproveInstructor
//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> ApproveInstructor(string userId)
//         {
//             if (string.IsNullOrEmpty(userId))
//             {
//                 return NotFound();
//             }

//             var user = await _userManager.FindByIdAsync(userId);
//             if (user == null)
//             {
//                 return NotFound();
//             }

//             user.IsApproved = true;
//             user.EmailConfirmed = true; // Optionally auto-confirm email on admin approval
//             var result = await _userManager.UpdateAsync(user);

//             if (result.Succeeded)
//             {
//                 _logger.LogInformation($"Instructor {user.Email} approved by admin.");
//                 // TODO: Send an email to the user letting them know they are approved
//             }

//             return RedirectToAction("ListUsers");
//         }

//         // GET: /Admin/CreateUser
//         public async Task<IActionResult> CreateUser()
//         {
//             // Get all roles *except* Admin (admins shouldn't create other admins)
//             var roles = await _roleManager.Roles
//                                 .Where(r => r.Name != "Admin")
//                                 .ToListAsync();

//             ViewData["Roles"] = new SelectList(roles, "Name", "Name");
//             return View();
//         }

//         // POST: /Admin/CreateUser
//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> CreateUser(AdminCreateUserViewModel model)
//         {
//             if (ModelState.IsValid)
//             {
//                 var user = new ApplicationUser
//                 {
//                     UserName = model.Email,
//                     Email = model.Email,
//                     FirstName = model.FirstName,
//                     LastName = model.LastName,
//                     IsApproved = true, // Admin-created accounts are pre-approved
//                     EmailConfirmed = true // Admin-created accounts are pre-confirmed
//                 };

//                 var result = await _userManager.CreateAsync(user, model.Password);

//                 if (result.Succeeded)
//                 {
//                     // Add to selected role
//                     if (!string.IsNullOrEmpty(model.Role))
//                     {
//                         await _userManager.AddToRoleAsync(user, model.Role);
//                     }

//                     _logger.LogInformation($"Admin created new user {user.Email} with role {model.Role}.");
//                     return RedirectToAction("ListUsers");
//                 }
//                 foreach (var error in result.Errors)
//                 {
//                     ModelState.AddModelError(string.Empty, error.Description);
//                 }
//             }

//             // Reload roles if model state is invalid
//             var roles = await _roleManager.Roles
//                                 .Where(r => r.Name != "Admin")
//                                 .ToListAsync();
//             ViewData["Roles"] = new SelectList(roles, "Name", "Name", model.Role);
//             return View(model);
//         }
//     }
// }


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using ScheduleCentral.Models.ViewModels;

namespace ScheduleCentral.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }
        // GET: /Admin/ListUsers
        public async Task<IActionResult> ListUsers(string? searchQuery, string? roleFilter)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            // 1. Apply Search Logic
            if (!string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = searchQuery.Trim();
                usersQuery = usersQuery.Where(u =>
                    (u.Email != null && u.Email.Contains(searchQuery)) ||
                    (u.FirstName != null && u.FirstName.Contains(searchQuery)) ||
                    (u.LastName != null && u.LastName.Contains(searchQuery)) ||
                    (u.UserName != null && u.UserName.Contains(searchQuery)));
            }

            // Execute query to get users list first
            var users = await usersQuery.ToListAsync();
            var userViewModels = new List<UserListViewModel>();

            // 2. Build ViewModel and Apply Role Filter (Memory-side)
            // We do this in memory because roles are stored in a separate table joined via UserRoles
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Apply Role Filter
                if (!string.IsNullOrEmpty(roleFilter) && !roles.Any(r => string.Equals(r, roleFilter, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                userViewModels.Add(new UserListViewModel
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email,
                    Roles = roles.ToList(),
                    IsActive = user.IsApproved
                });
            }

            // Populate ViewDatas for the Filter Dropdowns and Search Box
            ViewData["CurrentFilter"] = searchQuery;
            
            var allRoles = await _roleManager.Roles
                .Where(r => r.Name != null && r.Name.Trim() != "")
                .Select(r => r.Name!)
                .ToListAsync();
            ViewData["Roles"] = new SelectList(allRoles, roleFilter);

            return View(userViewModels);
        }
        [HttpGet]
        public async Task<IActionResult> GetCreateUserPartial()
        {
            // Exclude Admin from easy creation if desired, or include all
            var roles = await _roleManager.Roles
                .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                .Select(r => r.Name!)
                .ToListAsync();
            ViewBag.Roles = roles.Select(r => new SelectListItem { Value = r, Text = r }).ToList();
            
            return PartialView("_CreateUserModal", new AdminCreateUserViewModel());
        }
        // POST: /Admin/CreateUser
        // This action receives the form submission from the "Create User" Modal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(AdminCreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Email = (model.Email ?? "").Trim();

                var selected = (model.SelectedRoles ?? new List<string>())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!selected.Any())
                {
                    ModelState.AddModelError(nameof(model.SelectedRoles), "Select at least one role.");
                    var selectedRolesSet1 = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    var rolesForError = await _roleManager.Roles
                        .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                        .Select(r => r.Name!)
                        .ToListAsync();
                    ViewBag.Roles = rolesForError
                        .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSet1.Contains(r) })
                        .ToList();
                    return PartialView("_CreateUserModal", model);
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError(nameof(model.Email), "Email is required.");
                    var rolesForError = await _roleManager.Roles
                        .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                        .Select(r => r.Name!)
                        .ToListAsync();
                    var selectedRolesSetEmail = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    ViewBag.Roles = rolesForError
                        .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSetEmail.Contains(r) })
                        .ToList();
                    return PartialView("_CreateUserModal", model);
                }

                var existingByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingByEmail != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Already exists.");
                    var rolesForError = await _roleManager.Roles
                        .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                        .Select(r => r.Name!)
                        .ToListAsync();
                    var selectedRolesSetDup = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    ViewBag.Roles = rolesForError
                        .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSetDup.Contains(r) })
                        .ToList();
                    return PartialView("_CreateUserModal", model);
                }

                var existingByUserName = await _userManager.FindByNameAsync(model.Email);
                if (existingByUserName != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Already exists.");
                    var rolesForError = await _roleManager.Roles
                        .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                        .Select(r => r.Name!)
                        .ToListAsync();
                    var selectedRolesSetDup = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    ViewBag.Roles = rolesForError
                        .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSetDup.Contains(r) })
                        .ToList();
                    return PartialView("_CreateUserModal", model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    IsApproved = true, // Admin-created users are approved by default
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    var addRolesResult = await _userManager.AddToRolesAsync(user, selected);
                    if (!addRolesResult.Succeeded)
                    {
                        foreach (var error in addRolesResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        await _userManager.DeleteAsync(user);

                        var selectedRolesSet2 = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                        var rolesForError = await _roleManager.Roles
                            .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                            .Select(r => r.Name!)
                            .ToListAsync();
                        ViewBag.Roles = rolesForError
                            .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSet2.Contains(r) })
                            .ToList();
                        return PartialView("_CreateUserModal", model);
                    }
                    
                    _logger.LogInformation($"Admin created new user {user.Email}.");
                    
                    // Return JSON success to trigger the modal close and page reload in JS
                    return Json(new { success = true });
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If validation fails, reload roles and return the partial (with errors) to be re-rendered in the modal
            var selectedRolesSet3 = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var roles = await _roleManager.Roles
                .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                .Select(r => r.Name!)
                .ToListAsync();
            ViewBag.Roles = roles
                .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSet3.Contains(r) })
                .ToList();
            return PartialView("_CreateUserModal", model);
        }

        // GET: /Admin/GetManageUserPartial?id=...
        // Called via AJAX when "Manage" button is clicked
        [HttpGet]
        public async Task<IActionResult> GetManageUserPartial(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var isAdminUser = userRoles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase));
            
            // Get all roles except Admin for the dropdown
            var allRoles = await _roleManager.Roles
                .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                .Select(r => r.Name!)
                .ToListAsync();

            var nonAdminUserRoles = userRoles
                .Where(r => !string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var selectedRolesSet = new HashSet<string>(nonAdminUserRoles, StringComparer.OrdinalIgnoreCase);

            var model = new AdminManageUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}",
                IsActive = user.IsApproved,
                IsAdminUser = isAdminUser,
                SelectedRoles = nonAdminUserRoles,
                Roles = allRoles
                    .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSet.Contains(r) })
                    .ToList()
            };
            

            // Returns the PartialView directly into the modal body
            return PartialView("_ManageUserModal", model);
        }

        // POST: /Admin/ManageUser
        // Handles saving changes from the "Manage User" Modal (Save & Delete commands)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUser(AdminManageUserViewModel model, string? command)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                // Nothing to manage; consider this a success so the UI can refresh
                return Json(new { success = true });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var isAdminUser = currentRoles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase));
            model.IsAdminUser = isAdminUser;

            // 0. Handle Delete command
            if (string.Equals(command, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                // Safety: do not allow deleting Admin users via this modal
                if (isAdminUser)
                {
                    ModelState.AddModelError(string.Empty, "Cannot delete an Admin user.");

                    var allRolesDelete = await _roleManager.Roles
                        .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                        .Select(r => r.Name!)
                        .ToListAsync();

                    var nonAdminRoles = currentRoles
                        .Where(r => !string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    var selectedRolesSetDelete = new HashSet<string>(nonAdminRoles, StringComparer.OrdinalIgnoreCase);

                    model.Email = user.Email;
                    model.FullName = $"{user.FirstName} {user.LastName}";
                    model.IsActive = user.IsApproved;
                    model.SelectedRoles = nonAdminRoles;
                    model.Roles = allRolesDelete
                        .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSetDelete.Contains(r) })
                        .ToList();

                    return PartialView("_ManageUserModal", model);
                }

                var deleteResult = await _userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    foreach (var error in deleteResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    var allRolesDeleteError = await _roleManager.Roles
                        .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                        .Select(r => r.Name!)
                        .ToListAsync();

                    var nonAdminRoles = currentRoles
                        .Where(r => !string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    var selectedRolesSetDelete = new HashSet<string>(nonAdminRoles, StringComparer.OrdinalIgnoreCase);

                    model.Email = user.Email;
                    model.FullName = $"{user.FirstName} {user.LastName}";
                    model.IsActive = user.IsApproved;
                    model.SelectedRoles = nonAdminRoles;
                    model.Roles = allRolesDeleteError
                        .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSetDelete.Contains(r) })
                        .ToList();

                    return PartialView("_ManageUserModal", model);
                }

                return Json(new { success = true });
            }

            // If model state is invalid, re-render the modal with errors
            if (!ModelState.IsValid)
            {
                var allRolesInvalid = await _roleManager.Roles
                    .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                    .Select(r => r.Name!)
                    .ToListAsync();
                var selectedRolesSetInvalid = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                model.Roles = allRolesInvalid
                    .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSetInvalid.Contains(r) })
                    .ToList();
                return PartialView("_ManageUserModal", model);
            }

            // 1. Update Active/Deactive Status
            if (user.IsApproved != model.IsActive)
            {
                user.IsApproved = model.IsActive;
                // Invalidate user session if deactivated
                if (!model.IsActive)
                {
                    await _userManager.UpdateSecurityStampAsync(user);
                }
                await _userManager.UpdateAsync(user);
            }

            // 2. Update Roles (multi-role, Admin protected)
            var selectedRoles = (model.SelectedRoles ?? new List<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Where(r => !string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!isAdminUser && !selectedRoles.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedRoles), "Select at least one role.");
                var allRoles = await _roleManager.Roles
                    .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                    .Select(r => r.Name!)
                    .ToListAsync();
                var selectedRolesSet = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                model.Roles = allRoles
                    .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSet.Contains(r) })
                    .ToList();
                return PartialView("_ManageUserModal", model);
            }

            // Validate each selected role exists
            foreach (var roleName in selectedRoles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    ModelState.AddModelError(nameof(model.SelectedRoles), "One or more selected roles are invalid.");
                    var allRoles = await _roleManager.Roles
                        .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                        .Select(r => r.Name!)
                        .ToListAsync();
                    var selectedRolesSet = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    model.Roles = allRoles
                        .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSet.Contains(r) })
                        .ToList();
                    return PartialView("_ManageUserModal", model);
                }
            }

            var protectedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin" };
            var rolesChanged = false;

            var selectedRolesSetNormalized = new HashSet<string>(selectedRoles, StringComparer.OrdinalIgnoreCase);

            // Remove any non-Admin roles that are no longer selected
            var rolesToRemove = currentRoles
                .Where(r => !protectedRoles.Contains(r) && !selectedRolesSetNormalized.Contains(r))
                .ToList();

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    var allRoles = await _roleManager.Roles
                        .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                        .Select(r => r.Name!)
                        .ToListAsync();
                    var selectedRolesSet = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    model.Roles = allRoles
                        .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSet.Contains(r) })
                        .ToList();
                    return PartialView("_ManageUserModal", model);
                }

                rolesChanged = true;
            }

            // Add any newly selected non-Admin roles the user does not yet have
            var currentNonAdminRoles = currentRoles
                .Where(r => !protectedRoles.Contains(r))
                .ToList();

            var rolesToAdd = selectedRolesSetNormalized
                .Where(r => !currentNonAdminRoles.Any(cr => string.Equals(cr, r, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    foreach (var error in addResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    var allRoles = await _roleManager.Roles
                        .Where(r => r.Name != null && r.Name.Trim() != "" && r.Name != "Admin")
                        .Select(r => r.Name!)
                        .ToListAsync();
                    var selectedRolesSet = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    model.Roles = allRoles
                        .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSet.Contains(r) })
                        .ToList();
                    return PartialView("_ManageUserModal", model);
                }

                rolesChanged = true;
            }

            if (rolesChanged)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            // 3. Optional: Reset Password (Admin)
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var pwResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);
                if (!pwResult.Succeeded)
                {
                    foreach (var error in pwResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    var allRoles = await _roleManager.Roles
                        .Where(r => r.Name != null && r.Name != "Admin")
                        .Select(r => r.Name!)
                        .ToListAsync();
                    var selectedRolesSet = new HashSet<string>(model.SelectedRoles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    model.Roles = allRoles
                        .Select(r => new SelectListItem { Value = r, Text = r, Selected = selectedRolesSet.Contains(r) })
                        .ToList();
                    return PartialView("_ManageUserModal", model);
                }

                // Force re-login for the user after password change
                await _userManager.UpdateSecurityStampAsync(user);
            }

            return Json(new { success = true });
        }
        
        // GET: /Admin/ManageDepartments
        public async Task<IActionResult> ManageDepartments()
        {
            var depts = await _context.Departments
                .Include(d => d.Head)
                .Include(d => d.Batches)
                .AsNoTracking()
                .ToListAsync();

            // Populate department courses by matching the string department name
            foreach (var d in depts)
            {
                d.Courses = await _context.Courses
                    .Where(c => c.Department == d.Name)
                    .AsNoTracking()
                    .ToListAsync();
            }

            // Populate potential heads (Department role users only)
            var deptHeads = await _userManager.GetUsersInRoleAsync("Department");
            var users = deptHeads.Select(u => new { u.Id, Name = u.FullName }).ToList();
            ViewBag.PotentialHeads = new SelectList(users, "Id", "Name");

            return View(depts);
        }

        // POST: /Admin/CreateDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(Department department)
        {
            if (ModelState.IsValid)
            {
                department.Name = (department.Name ?? "").Trim();
                department.Code = (department.Code ?? "").Trim();

                if (string.IsNullOrWhiteSpace(department.Name))
                {
                    TempData["Error"] = "Department name is required.";
                    return RedirectToAction(nameof(ManageDepartments));
                }

                var nameExists = await _context.Departments
                    .AsNoTracking()
                    .AnyAsync(d => d.Name != null && d.Name.ToLower() == department.Name.ToLower());

                if (nameExists)
                {
                    TempData["Error"] = "Already exists.";
                    return RedirectToAction(nameof(ManageDepartments));
                }

                if (!string.IsNullOrWhiteSpace(department.Code))
                {
                    var codeExists = await _context.Departments
                        .AsNoTracking()
                        .AnyAsync(d => d.Code != null && d.Code.ToLower() == department.Code.ToLower());

                    if (codeExists)
                    {
                        TempData["Error"] = "Already exists.";
                        return RedirectToAction(nameof(ManageDepartments));
                    }
                }

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                // Assign 'Department' role to the head if they don't have it
                if (!string.IsNullOrEmpty(department.HeadId))
                {
                    var head = await _userManager.FindByIdAsync(department.HeadId);
                    if (head != null && !await _userManager.IsInRoleAsync(head, "Department"))
                    {
                        await _userManager.AddToRoleAsync(head, "Department");
                    }
                }
                TempData["Success"] = "Department created successfully.";
            }
            else 
            {
                TempData["Error"] = "Failed to create department.";
            }
            return RedirectToAction(nameof(ManageDepartments));
        }
        // GET: /Admin/GetEditDepartmentPartial
        [HttpGet]
        public async Task<IActionResult> GetEditDepartmentPartial(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();

            // Populate potential heads again
            var deptHeads = await _userManager.GetUsersInRoleAsync("Department");
            var users = deptHeads.Select(u => new { u.Id, Name = u.FullName }).ToList();
            ViewBag.PotentialHeads = new SelectList(users, "Id", "Name", dept.HeadId);

            return PartialView("_EditDepartmentModal", dept);
        }

        // POST: /Admin/EditDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartment(Department department)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.Departments.FindAsync(department.Id);
                if (existing == null) return NotFound();

                var oldName = existing.Name;

                existing.Name = department.Name;
                existing.Code = department.Code;

                // If the department name changes, keep string-based references in sync.
                if (!string.Equals(oldName, existing.Name, StringComparison.Ordinal))
                {
                    var courses = await _context.Courses.Where(c => c.Department == oldName).ToListAsync();
                    foreach (var c in courses)
                    {
                        c.Department = existing.Name;
                    }

                    var offerings = await _context.CourseOfferings.Where(o => o.Department == oldName).ToListAsync();
                    foreach (var o in offerings)
                    {
                        o.Department = existing.Name;
                    }

                    var usersToUpdate = await _context.Users.Where(u => u.Department == oldName).ToListAsync();
                    foreach (var u in usersToUpdate)
                    {
                        u.Department = existing.Name;
                    }
                }

                // Handle Head change logic
                if (existing.HeadId != department.HeadId)
                {
                    // 1. Remove role from old head (Optional, business rule dependent)
                    // if (!string.IsNullOrEmpty(existing.HeadId)) { ... }

                    // 2. Add role to new head
                    if (!string.IsNullOrEmpty(department.HeadId))
                    {
                        var newHead = await _userManager.FindByIdAsync(department.HeadId);
                        if (newHead != null && !await _userManager.IsInRoleAsync(newHead, "Department"))
                        {
                            await _userManager.AddToRoleAsync(newHead, "Department");
                        }
                    }
                    existing.HeadId = department.HeadId;
                }

                _context.Update(existing);
                await _context.SaveChangesAsync();

                //TempData["Success"] = "Department updated successfully.";
                //return RedirectToAction(nameof(ManageDepartments));

                // RETURN JSON FOR AJAX
                return Json(new { success = true });
            }
            //TempData["Error"] = "Failed to update department.";
            //return RedirectToAction(nameof(ManageDepartments));

            // If failure, reload dropdown and return partial
            var deptHeads = await _userManager.GetUsersInRoleAsync("Department");
            var users = deptHeads.Select(u => new { u.Id, Name = u.FullName }).ToList();
            ViewBag.PotentialHeads = new SelectList(users, "Id", "Name", department.HeadId);
            return PartialView("_EditDepartmentModal", department);
        }

        // GET: /Admin/GetDeleteDepartmentPartial
        [HttpGet]
        public async Task<IActionResult> GetDeleteDepartmentPartial(int id)
        {
            var dept = await _context.Departments
                .Include(d => d.Batches)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dept == null) return NotFound();
            dept.Courses = await _context.Courses
                .Where(c => c.Department == dept.Name)
                .AsNoTracking()
                .ToListAsync();
            return PartialView("_DeleteDepartmentModal", dept);
        }

        // POST: /Admin/DeleteDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept != null)
            {
                // Optional: Check for dependencies before delete to prevent FK errors
                // For now, we assume cascade delete or let EF handle the exception
                _context.Departments.Remove(dept);
                await _context.SaveChangesAsync();
                // RETURN JSON FOR AJAX
                return Json(new { success = true });
            }

            return NotFound();
        }
    }
}