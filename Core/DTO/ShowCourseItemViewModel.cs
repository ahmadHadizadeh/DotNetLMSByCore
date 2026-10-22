using System;
using System.Collections.Generic;
using DataLayer.Course;
using DataLayer.Entities.Course;
using DataLayer.Entities.Order;

namespace Core.DTO
{
    public class ShowCourseItemViewModel
    {
        public Course Course { get; set; }
        public List<UserCourse> UserCourse { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string ImageName { get; set; }
        public int Price { get; set; }
        public TimeSpan TotalTime { get; set; }
        public List<CourseEpisode> CourseEpisodes { get; set; }
    }
    public class ShowCourseCreateQuestionViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string ImageCourse { get; set; }
       
    }
}