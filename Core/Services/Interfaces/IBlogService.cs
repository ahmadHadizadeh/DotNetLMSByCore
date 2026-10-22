using Core.DTO;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using DataLayer.Entities.Bloger;

namespace Core.Services.Interfaces
{
    public interface IBlogService
    {
        List<ShowBlogViewModel> GetShowBlog();
        ShowBlogViewModel GetListBySHowBlog(int pageId = 1);
        int AddBlog(Blog blog, IFormFile blogImage);

        Blog GetBlogById(int id);
        void UpdateBlogById(Blog blog);
        void UpdateBlog(Blog blog, IFormFile blogImage);

        List<ShowBlogViewModel> GetBlogShowMainIndex();

        Blog GetBlogJustClickOnBlog(int blogId);

        void AddCommentBlog(CommentBlog comment);

        Tuple<List<CommentBlog>, int> GetCommentByShowBlog(int blogId, int pageId = 1);

        Tuple<List<ShowBlogViewModel>, int> GetAllBlogByShowToIndex(int pageId = 1, int take = 0, string filter = "", string orderBy = "date");
        void DeleteBlog(int blogId);
        DeleteBlogViewModel GetDeleteBlog(int blogId);
        Blog GetShortKeyRedirectBlog(string key);
        string GenerateShortKeyForBlog(int lenght = 4);
        List<ShowBlogToCreateQuestionViewModel> GetBlogSidbarToShowBlog();
    }
}