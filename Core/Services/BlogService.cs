using Core.Convertors;
using Core.DTO;
using Core.Genarate;
using Core.Security;
using Core.Services.Interfaces;
using DataLayer.Context;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataLayer.Entities.Bloger;

namespace Core.Services
{
    public class BlogService : IBlogService
    {
        private AsanwebContext _context;

        public BlogService(AsanwebContext context)
        {
            _context = context;
        }

        public List<ShowBlogViewModel> GetShowBlog()
        {
            return _context.Blog.Select(b => new ShowBlogViewModel()
            {
                BlogId = b.BlogId,
                AbstractText = b.AbstractText,
                BlogDateTime = b.Date,
                BlogImage = b.BlogImageName,
                BlogText = b.TextBlog,
                BlogTitle = b.Title,
                BlogWriter = b.Writer
            }).ToList();
        }

        public int AddBlog(Blog blog, IFormFile BlogImageName)
        {
            if (BlogImageName != null && BlogImageName.IsImage())
            {
                //image name
                blog.BlogImageName = CreateUniqCode.buildUniqCod() + Path.GetExtension(BlogImageName.FileName);
                //Image Path
                string mainImagepath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Blog/MainImage", blog.BlogImageName);
                //save
                using (var stream = new FileStream(mainImagepath, FileMode.Create))
                {
                    BlogImageName.CopyTo(stream);
                }
                //resize
                ImageConvertor imagResizer = new ImageConvertor();
                string thumPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Blog/Thumnail",
                    blog.BlogImageName);
                imagResizer.Image_resize(mainImagepath, thumPath, 500);
                //add
            }
            blog.Date = DateTime.Now;
            _context.Add(blog);
            _context.SaveChanges();
            return blog.BlogId;
        }

        public Blog GetBlogById(int id)
        {
            return _context.Blog.Find(id);
        }

        public void UpdateBlog(Blog blog, IFormFile BlogImageName)
        {
            if (BlogImageName != null && BlogImageName.IsImage())
            {
                if (blog.BlogImageName != "Default.png")
                {
                    //main img
                    string mainImagepath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Blog/MainImage", blog.BlogImageName);
                    if (File.Exists(mainImagepath))
                    {
                        File.Delete(mainImagepath);
                    }
                    //deletee path

                    //Thumimg img
                    string thumPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Blog/Thumnail", blog.BlogImageName);
                    //deletee path
                    if (File.Exists(thumPath))
                    {
                        File.Delete(thumPath);
                    }
                    //img name
                    blog.BlogImageName = CreateUniqCode.buildUniqCod() + Path.GetExtension(BlogImageName.FileName);

                    //imge path
                    string mainPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Blog/MainImage", blog.BlogImageName);

                    //save
                    using (var stream = new FileStream(mainPath, FileMode.Create))
                    {
                        BlogImageName.CopyTo(stream);
                    }
                    //resize
                    string thumPathImage = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Blog/Thumnail", blog.BlogImageName);
                    ImageConvertor imgresizer = new ImageConvertor();
                    imgresizer.Image_resize(mainPath, thumPathImage, 400);
                }
            }
            blog.Date = DateTime.Now;
            _context.Blog.Update(blog);
            _context.SaveChanges();
        }

        public void UpdateBlogById(Blog blog)
        {
            _context.Blog.Update(blog);
            _context.SaveChanges();
        }
        public void DeleteBlog(int blogId)
        {
            var blog = GetBlogById(blogId);
            blog.IsDelete = true;
            UpdateBlogById(blog);
        }
        public DeleteBlogViewModel GetDeleteBlog(int blogId)
        {
            var blog = GetBlogById(blogId);
            DeleteBlogViewModel info = new DeleteBlogViewModel();
            info.BlogTitle = blog.Title;
            info.BlogWriter = blog.Writer;
            info.ImageName = blog.BlogImageName;

            return info;
        }

       public ShowBlogViewModel GetListBySHowBlog(int pageId = 1)
        {
            IQueryable<Blog> result = _context.Blog;
            int take = 10;
            int skip = (pageId - 1) * take;
            ShowBlogViewModel blog = new ShowBlogViewModel();
            blog.Blog = result.OrderByDescending(c => c.Date).Skip(skip).Take(take).ToList();
            blog.PageCount = result.Count() / take;
            if ((blog.PageCount % 2) != 0)
            {
                blog.PageCount++;
            }
            return blog;
        }


        public List<ShowBlogViewModel> GetBlogShowMainIndex()
        {
            return _context.Blog.Select(b => new ShowBlogViewModel()
            {
                BlogId = b.BlogId,
                AbstractText = b.AbstractText,
                BlogDateTime = b.Date,
                BlogImage = b.BlogImageName,
                BlogText = b.TextBlog,
                BlogTitle = b.Title,
                BlogWriter = b.Writer
            }).OrderByDescending(b => b.BlogDateTime).Take(6).ToList();
        }

        public Blog GetBlogJustClickOnBlog(int blogId)
        {
            return _context.Blog.SingleOrDefault(b => b.BlogId == blogId);
        }

        public void AddCommentBlog(CommentBlog comment)
        {
            _context.CommentBlog.Add(comment);
            _context.SaveChanges();
        }

        public Tuple<List<CommentBlog>, int> GetCommentByShowBlog(int blogId, int pageId = 1)
        {
            int take = 5;
            int skip = (pageId - 1) * take;
            int pageCount = _context.CommentBlog.Count(c => !c.IsDelete && c.BlogId == blogId) / take;
            if ((pageCount % 2) != 0)
            {
                pageCount++;
            }
            //Shocomment
            return Tuple.Create(_context.CommentBlog.OrderByDescending(c => c.CreateDate).Include(c => c.User)
                .Where(c => !c.IsDelete && c.BlogId == blogId).Skip(skip).Take(take).ToList(), pageCount);
        }

        public Tuple<List<ShowBlogViewModel>, int> GetAllBlogByShowToIndex(int pageId = 1, int take = 0, string filter = "", string orderBy = "date")
        {
            if (take == 0)
            {
                take = 10;
            }

            IQueryable<Blog> result = _context.Blog;
            if (!string.IsNullOrEmpty(filter))
            {
                result = result.Where(b => b.Title.Contains(filter) || b.Tags.Contains(filter));
            }

            switch (orderBy)
            {
                case "date":
                    {
                        result = result.OrderByDescending(b => b.Date);
                        break;
                    }
                case "title":
                    {
                        result = result.OrderByDescending(b => b.Title);
                        break;
                    }
            }

            int skip = (pageId - 1) * take;
            var pageCount = result.Select(b => new ShowBlogViewModel()
            {
                BlogTitle = b.Title,
                BlogDateTime = b.Date,
                BlogImage = b.BlogImageName,
                BlogWriter = b.Writer,
                BlogText = b.TextBlog,
                AbstractText = b.AbstractText,
            }).Count() / take;

            var query = result.Select(b => new ShowBlogViewModel()
            {

                BlogTitle = b.Title,
                BlogDateTime = DateTime.Now,
                BlogImage = b.BlogImageName,
                BlogWriter = b.Writer,
                BlogText = b.TextBlog,
                AbstractText = b.AbstractText,
                BlogId = b.BlogId
            }).Skip(skip).Take(take).ToList();
            return Tuple.Create(query, pageCount);
        }
        public Blog GetShortKeyRedirectBlog(string key)
        {
            return _context.Blog.FirstOrDefault(c => c.ShortKey == key);
        }

        public string GenerateShortKeyForBlog(int lenght = 4)
        {
            string key = Guid.NewGuid().ToString().Replace("-", "").Substring(0, lenght);

            while (_context.Course.Any(s => s.ShortKey == key))
            {
                key = Guid.NewGuid().ToString().Replace("-", "").Substring(0, lenght);
            }

            return key;
        }

        public List<ShowBlogToCreateQuestionViewModel> GetBlogSidbarToShowBlog()
        {
            return _context.Blog.OrderByDescending(b=>b.Date).Select(b => new ShowBlogToCreateQuestionViewModel()
            {
                BlogId = b.BlogId,
                BlogTitles = b.Title,
                BlogImage = b.BlogImageName,
                DateBlog =b.Date

            }).Take(20).ToList();
        }

    }
}