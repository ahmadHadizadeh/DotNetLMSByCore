using System;
using System.Collections.Generic;
using DataLayer.Entities.Bloger;

namespace Core.DTO
{
    public class BlogViewModel
    {
        public List<Blog> Blog { get; set; }
        public int BlogId { get; set; }
        public string BlogTitle { get; set; }
        public string ImageName { get; set; }
        public string Description { get; set; }
        public string Abstracttext { get; set; }
        public string Writer { get; set; }
        public DateTime CreateDate { get; set; }
        public int PageCount { get; set; }
        public int CurrentPage { get; set; }
    }
    public class ShowBlogViewModel
    {
        public List<Blog> Blog { get; set; }
        public int BlogId { get; set; }
        public string BlogTitle { get; set; }
        public string BlogImage { get; set; }
        public string BlogText { get; set; }
        public string AbstractText { get; set; }
        public string BlogWriter { get; set; }
        public DateTime BlogDateTime { get; set; }
        public int PageCount { get; set; }
        public int CurrentPage { get; set; }
    }
    public class DeleteBlogViewModel
    {
        public string BlogTitle { get; set; }
        public string BlogWriter { get; set; }
        public string ImageName { get; set; }
    }
    public class ShowBlogToCreateQuestionViewModel
    {
        public int BlogId { get; set; }
        public string BlogTitles { get; set; }
        public DateTime DateBlog  { get; set; }
        public string BlogImage { get; set; }

    }
}
