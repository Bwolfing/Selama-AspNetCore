﻿using Guilded.Areas.Forums.DAL;
using Guilded.Areas.Forums.ViewModels;
using Guilded.Data.Forums;
using Guilded.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Guilded.Areas.Forums.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(IForumsDataContext dataContext) : base(dataContext)
        {
        }

        public ViewResult Index()
        {
            var forumSections = DataContext.GetActiveForumSections();

            return View(forumSections.OrderBy(f => f.DisplayOrder)
                .ToList()
                .Select(f => new ForumSectionViewModel(f))
            );
        }

        [Route("{slug}")]
        public async Task<IActionResult> ForumBySlug(string slug, int page = 1)
        {
            if (page <= 0)
            {
                return RedirectToAction(nameof(ForumBySlug), new { slug });
            }

            var forum = await DataContext.GetForumBySlugAsync(slug);

            if (forum == null)
            {
                return NotFound();
            }

            var viewModel = CreatePaginatedForumViewModel(forum, page);

            if (viewModel.LastPage == 0 && page != 1)
            {
                return RedirectToAction(nameof(ForumBySlug), new {slug});
            }
            if (viewModel.LastPage != 0 && page > viewModel.LastPage)
            {
                return RedirectToAction(nameof(ForumBySlug), new {slug, page = viewModel.LastPage});
            }

            return ForumView(viewModel);
        }

        [Route("{id:int}")]
        public async Task<IActionResult> ForumById(int id)
        {
            var forum = await DataContext.GetForumByIdAsync(id);

            if (forum == null)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(ForumBySlug), new {slug = forum.Slug});
        }

        private ForumViewModel CreatePaginatedForumViewModel(Forum forum, int page)
        {
            var zeroIndexedPage = page - 1;
            var pinnedThreads = forum.Threads.Where(t => t.IsPinned).OrderByDescending(t => t.CreatedAt);
            var threads = forum.Threads.Where(t => !t.IsPinned).OrderByDescending(t => t.CreatedAt);

            return new ForumViewModel(forum)
            {
                CurrentPage = page,
                PagerUrl = Url.Action(nameof(ForumBySlug), new { slug = forum.Slug}),
                LastPage = (int)Math.Ceiling(forum.Threads.Count / (double)PageSize),
                PinnedThreads = pinnedThreads.ToList().Select(t => new ThreadOverviewViewModel(t)),
                Models = threads.Skip(zeroIndexedPage * PageSize)
                    .Take(PageSize)
                    .ToList()
                    .Select(t => new ThreadOverviewViewModel(t)),
            };
        }

        private ViewResult ForumView(ForumViewModel viewModel)
        {
            Breadcrumbs.Push(new Breadcrumb
            {
                Title = viewModel.Title,
                Url = Url.Action(nameof(ForumBySlug), new { viewModel.Slug })
            });

            return View(viewModel);
        }
    }
}