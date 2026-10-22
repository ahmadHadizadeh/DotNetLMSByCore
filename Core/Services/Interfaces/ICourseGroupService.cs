using Core.DTO;
using DataLayer.Course;
using DataLayer.Entities.Bloger;
using DataLayer.Entities.Course;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using Core.DTO.Banner;
using DataLayer.Entities.Banner;
using DataLayer.Entities.Order;

namespace Core.Services.Interfaces
{
    public interface ICourseGroupService
    {
        #region ListCourse

        List<CourseGroup> GetAllGroup();

        List<SelectListItem> GetMainGroup();

        List<SelectListItem> GetSubGroup(int groupId);

        List<SelectListItem> GetTeacher();

        List<SelectListItem> GetLevels();

        // List<SelectListItem> GetPlan();

        List<SelectListItem> GetStatus();

        #endregion ListCourse

        #region Course

        int AddCourse(Course course, IFormFile courseImage, IFormFile Demo);

        ShowListForCourse GetListCourse(int pageId = 1, string filterTitle = "");

        Course GetCourseByCourseId(int courseId);

        void UpdateCourse(Course course, IFormFile courseImage, IFormFile Demo);

        int AddCourseEpisode(CourseEpisode episode, IFormFile episodeFile);

        bool IsExistFile(string fileName);

        List<CourseEpisode> GetListEpisode(int courseId);

        CourseEpisode GetEpisodeById(int episodeid);

        void EditEpisode(CourseEpisode episode, IFormFile episodefile);

        Tuple<List<ShowCourseItemViewModel>, int> GetCourse(int pageId = 1, int take = 0, string filter = "",
            string typeShow = "all", string orderBy = "date",
            int startPrice = 0, int endPrice = 0, List<int> selectedGroup = null);

        List<ShowCourseCreateQuestionViewModel> GetCourseCreateQuestion();

        Course GetCourseInfoByShowCourseDetailse(int courseId);

        void AddComment(CourseComment comment);

        Tuple<List<CourseComment>, int> GetListComment(int courseId, int pageId = 1);

        #region Teacher

        bool AddEpisode(AddEpisodeTeacherViewModel episodeViewModel, string userName);

        List<Course> GetAllCourseTeacher(string teacherName);

        List<CourseEpisode> GetEpisodeByCourseId(int courseId);

        #endregion Teacher

        #endregion Course

        #region CoursePopular

        List<ShowCourseItemViewModel> GetCoursePopular();

        #endregion CoursePopular

        #region AdminCourseGroup

        List<CourseGroup> GetAllCourseGroups();

        AdminCourseViewModel GetDeleteCourse(int pageId = 1, string filterCourseName = "");

        InfoCourseViewModel GetCourseByListDelete(int courseId);

        void DeleteCourse(int courseId);

        void UpdateCourse(Course course);

        void AddCourseGroup(CourseGroup courseGroup);

        CourseGroup GetCourseGroupById(int groupId);

        void UpdateCourseGroup(CourseGroup courseGroup);

        #endregion AdminCourseGroup

        #region Episode

        InfoShowEpisodeDeleteViewModel GetEpisodeByDelete(int episodeId);

        CourseEpisode GetCourseEpisodeByEpisodeId(int episodeId);

        void UpdateEpisode(CourseEpisode episode);

        void DeleteEpisode(int episodeId, IFormFile filename);

        #endregion Episode

        #region Vote

        void AddsVote(int userId, int courseId, bool vote);

        Tuple<int, int> GetCourseVotes(int courseId);

        bool IsFree(int courseId);

        #endregion Vote

        #region ShortLink

        string GenerateShortKey(int lenght = 4);

        Course GetShortKeyRedirect(string key);

        #endregion ShortLink

        #region Banner

        int AddBanner(BannerInformation banner, IFormFile bannerImage);

        List<BannerInformation> GetAllBannerForIndex();

        BannerInformation GetBannerId(int bannerId);

        void DeleteBanner(int bannerId);

        void UpdateBanner(BannerInformation banner);

        BannerInfoViewModel GetBannerForDelete(int bannerId);

        BannerInfoViewModel GetBannerToIndex();

        List<BannerInfoViewModel> GetBannerForIndex();

        #endregion Banner
    }
}