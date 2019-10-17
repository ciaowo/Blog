﻿using MeowvBlog.Core;
using MeowvBlog.Core.Dto;
using MeowvBlog.Core.Dto.Blog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MeowvBlog.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = GlobalConsts.GroupName_v1)]
    public class BlogController : ControllerBase
    {
        private readonly MeowvBlogDBContext _context;

        public BlogController(MeowvBlogDBContext context)
        {
            _context = context;
        }

        #region Posts

        /// <summary>
        /// 根据URL获取文章详情
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("post")]
        public async Task<Response<GetPostDto>> GetPostAsync(string url)
        {
            var response = new Response<GetPostDto>();

            var post = await _context.Posts.FirstOrDefaultAsync(x => x.Url.Equals(url));
            if (null == post)
            {
                response.Msg = $"URL：{url} 不存在";
                return response;
            }
            var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == post.CategoryId);
            var tags = (from post_tags in await _context.PostTags.ToListAsync()
                        join tag in await _context.Tags.ToListAsync()
                        on post_tags.TagId equals tag.Id
                        where post_tags.PostId.Equals(post.Id)
                        select new TagDto
                        {
                            TagName = tag.TagName,
                            DisplayName = tag.DisplayName
                        }).ToList();
            var previous = await _context.Posts.Where(x => x.CreationTime > post.CreationTime).Take(1).FirstOrDefaultAsync();
            var next = await _context.Posts.Where(x => x.CreationTime < post.CreationTime).OrderByDescending(x => x.CreationTime).Take(1).FirstOrDefaultAsync();

            var result = new GetPostDto
            {
                Title = post.Title,
                Author = post.Author,
                Url = post.Url,
                Html = post.Html,
                Markdown = post.Markdown,
                CreationTime = Convert.ToDateTime(post.CreationTime).ToString("MMMM dd, yyyy HH:mm:ss", new CultureInfo("en-us")),
                Category = new CategoryDto { CategoryName = category.CategoryName, DisplayName = category.DisplayName },
                Tags = tags,
                Previous = new PostForPagedDto { Title = previous.Title, Url = previous.Url },
                Next = new PostForPagedDto { Title = next.Title, Url = next.Url }
            };

            response.Result = result;
            return response;
        }

        /// <summary>
        /// 分页查询文章列表
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("post/query")]
        public async Task<PagedResponse<QueryPostDto>> QueryPostsAsync([FromQuery] PagingInput input)
        {
            var posts = await _context.Posts.ToListAsync();
            var count = posts.Count;

            var result = posts.OrderByDescending(x => x.CreationTime)
                              .Skip((input.Page - 1) * input.Limit)
                              .Take(input.Limit)
                              .Select(x => new PostBriefDto
                              {
                                  Title = x.Title,
                                  Url = x.Url,
                                  Year = Convert.ToDateTime(x.CreationTime).Year,
                                  CreationTime = Convert.ToDateTime(x.CreationTime).ToString("MMMM dd, yyyy HH:mm:ss", new CultureInfo("en-us"))
                              })
                              .GroupBy(x => x.Year)
                              .Select(x => new QueryPostDto
                              {
                                  Year = x.Key,
                                  Posts = x.ToList()
                              }).ToList();

            return new PagedResponse<QueryPostDto>(count, result);
        }

        /// <summary>
        /// 通过标签名称查询文章列表
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("post/query_by_tag")]
        public async Task<IList<QueryPostDto>> QueryPostsByTagAsync(string name)
        {
            return (from post_tags in await _context.PostTags.ToListAsync()
                    join tags in await _context.Tags.ToListAsync()
                    on post_tags.TagId equals tags.Id
                    join posts in await _context.Posts.ToListAsync()
                    on post_tags.PostId equals posts.Id
                    where tags.DisplayName.Equals(name)
                    orderby posts.CreationTime descending
                    select new PostBriefDto
                    {
                        Title = posts.Title,
                        Url = posts.Url,
                        CreationTime = posts.CreationTime.ToString("MMMM dd, yyyy HH:mm:ss", new CultureInfo("en-us")),
                        Year = posts.CreationTime.Year
                    })
                    .GroupBy(x => x.Year)
                    .Select(x => new QueryPostDto
                    {
                        Year = x.Key,
                        Posts = x.ToList()
                    }).ToList();
        }

        /// <summary>
        /// 通过分类名称查询文章列表
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("post/query_by_category")]
        public async Task<IList<QueryPostDto>> QueryPostsByCategoryAsync(string name)
        {
            return (from posts in await _context.Posts.ToListAsync()
                    join categories in await _context.Categories.ToListAsync()
                    on posts.CategoryId equals categories.Id
                    where categories.DisplayName.Equals(name)
                    orderby posts.CreationTime descending
                    select new PostBriefDto
                    {
                        Title = posts.Title,
                        Url = posts.Url,
                        CreationTime = posts.CreationTime.ToString("MMMM dd, yyyy HH:mm:ss", new CultureInfo("en-us")),
                        Year = posts.CreationTime.Year
                    })
                    .GroupBy(x => x.Year)
                    .Select(x => new QueryPostDto
                    {
                        Year = x.Key,
                        Posts = x.ToList()
                    }).ToList();
        }

        #endregion Posts
    }
}