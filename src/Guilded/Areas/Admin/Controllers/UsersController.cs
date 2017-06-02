﻿using Guilded.Areas.Admin.Data.DAL;
using Guilded.Areas.Admin.ViewModels.Users;
using Guilded.Constants;
using Guilded.Identity;
using Guilded.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Guilded.Areas.Admin.Controllers
{
    public class UsersController : BaseController
    {
        public const int PageSize = 20;

        private readonly IUsersDataContext _usersDataContext;
        private readonly IRolesDataContext _rolesDataContext;
        private readonly ILogger _logger;

        public UsersController(IUsersDataContext usersDataContext,
            IRolesDataContext rolesDataContext,
            ILoggerFactory loggerFactory)
        {
            _usersDataContext = usersDataContext;
            _rolesDataContext = rolesDataContext;
            _logger = loggerFactory.CreateLogger<UsersController>();
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (page <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var viewModel = await GetUsers(page);

            if (viewModel.LastPage == 0 && page != 1)
            {
                return RedirectToAction(nameof(Index));
            }
            if (viewModel.LastPage != 0 && page > viewModel.LastPage)
            {
                return RedirectToAction(nameof(Index), new { page = viewModel.LastPage });
            }

            ViewData[ViewDataKeys.UserRolesSelectList] = _rolesDataContext.GetRoles()
                .Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Id,
                });

            return View(viewModel);
        }

        [HttpGet("[area]/[controller]/edit/{userId}")]
        public async Task<IActionResult> Edit(string userId)
        {
            var user = await _usersDataContext.GetUserByIdAsync(userId);

            if (user == null)
            {
                TempData[ViewDataKeys.ErrorMessages] = "That user doesn't appear to exist.";
                return RedirectToAction(nameof(Index));
            }

            return await UserEditorView(user);
        }

        [HttpPost("[area]/[controller]/{userId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableUser(string userId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _usersDataContext.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new
                {
                    message = $"Unable to find user with Id '{userId}'.",
                    userId
                });
            }

            user.IsEnabled = true;
            user.EnabledAfter = null;

            try
            {
                await _usersDataContext.UpdateUserAsync(user);
            }
            catch (Exception e)
            {
                _logger.LogError(EventIdRangeStart + 11, e.Message, e);
                return StatusCode(500, new
                {
                    message = $"There was en error enabling {user.UserName}.",
                    userId
                });
            }

            return Ok(new
            {
                message = $"{user.UserName} successfully enabled.",
                userId
            });
        }

        [HttpDelete("[area]/[controller]/{userId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableUser(DisableUserViewModel userToDisable)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var dbUser = await _usersDataContext.GetUserByIdAsync(userToDisable.Id);

            if (dbUser == null)
            {
                return NotFound(new
                {
                    message = $"Unable to find user with Id '{userToDisable.Id}'.",
                    userId = userToDisable.Id
                });
            }

            dbUser.IsEnabled = false;
            dbUser.EnabledAfter = userToDisable.EnableAfter;

            try
            {
                await _usersDataContext.UpdateUserAsync(dbUser);
            }
            catch (Exception e)
            {
                _logger.LogError(EventIdRangeStart + 10, e.Message, e);
                return StatusCode(500, new
                {
                    message = $"There was an error disabling {dbUser.UserName}.",
                    userId = userToDisable.Id
                });
            }

            return Ok(new
            {
                userId = userToDisable.Id,
                message = $"{dbUser.UserName} successfully disabled." +
                          (
                            userToDisable.EnableAfter.HasValue ?
                            $" {dbUser.UserName} will regain access on {userToDisable.EnableAfter.Value.Date.AddDays(1):d}." :
                            string.Empty
                          )
            });
        }

        public override ViewResult View(string viewName, object model)
        {
            Breadcrumbs.Push(new Breadcrumb
            {
                Title = "Users",
                Url = Url.Action(nameof(Index))
            });

            return base.View(viewName, model);
        }

        private async Task<ViewResult> UserEditorView(ApplicationUser user)
        {
            Breadcrumbs.Push(new Breadcrumb
            {
                Title = "Edit User",
                Url = Url.Action(nameof(Edit), new { userId = user.Id})
            });

            return View("Edit", new ApplicationUserViewModel(user, await _usersDataContext.GetRoleForUserAsync(user)));
        }

        private async Task<PaginatedViewModel<ApplicationUserViewModel>> GetUsers(int page)
        {
            var zeroIndexPage = page - 1;
            var allUsers = _usersDataContext.GetUsers().OrderBy(u => u.UserName);
            var usersForPage = allUsers.Skip(PageSize * zeroIndexPage).Take(PageSize);
            var viewModel = new PaginatedViewModel<ApplicationUserViewModel>
            {
                CurrentPage = page,
                LastPage = (int)Math.Ceiling(allUsers.Count() / (double)PageSize),
            };

            foreach (var user in usersForPage)
            {
                viewModel.Models.Add(new ApplicationUserViewModel(
                    user,
                    await _usersDataContext.GetRoleForUserAsync(user)
                ));
            }

            return viewModel;
        }
    }
}
