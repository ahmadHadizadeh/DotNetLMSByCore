using DataLayer.Course;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DataLayer.Entities.Course;

namespace Core.DTO
{
    public class ShowListForCourse
    {
        public int EpisodeCount { get; set; }
        public List<Course> Course { get; set; }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
    }
    public class AdminCourseViewModel
    {
        public List<Course> Course { get; set; }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
    }

    public class InfoCourseViewModel
    {
        public string Title { get; set; }
        public DateTime CreateDate { get; set; }
    }
    public class InfoShowEpisodeDeleteViewModel
    {
        public string EpisodeTitle { get; set; }
        public string Imagename { get; set; }
    }

    public class AddEpisodeTeacherViewModel
    {
        public int CourseId { get; set; }
        [Display(Name = "عنوان بخش")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(400, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد .")]
        public string EpisodeTitle { get; set; }

        [Display(Name = "زمان")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public TimeSpan EpisodeTime { get; set; }

        [Display(Name = "فایل")]
        public string EpisodeFileName { get; set; }

        [Display(Name = "رایگان")]
        public bool IsFree { get; set; }
    }
}