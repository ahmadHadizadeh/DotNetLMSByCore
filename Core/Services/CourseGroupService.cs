using Core.Convertors;
using Core.DTO;
using Core.Genarate;
using Core.Security;
using Core.Services.Interfaces;
using DataLayer.Context;
using DataLayer.Course;
using DataLayer.Entities.Bloger;
using DataLayer.Entities.Course;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.DTO.Banner;
using DataLayer.Entities.Banner;
using DataLayer.Entities.Order;

namespace Core.Services
{
    public class CourseGroupService : ICourseGroupService
    {
        #region Course

        private AsanwebContext _context;
        private IUserService _userService;

        public CourseGroupService(AsanwebContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public List<CourseGroup> GetAllGroup()
        {
            return _context.CourseGroup.ToList();
        }

        public List<SelectListItem> GetMainGroup()
        {
            return _context.CourseGroup.Where(g => g.ParentId == null)
                .Select(g => new SelectListItem()
                {
                    Text = g.GroupTitle,
                    Value = g.GroupId.ToString()
                }).ToList();
        }

        public List<SelectListItem> GetSubGroup(int groupId)
        {
            return _context.CourseGroup.Where(g => g.ParentId == groupId).Select(g => new SelectListItem()
            {
                Text = g.GroupTitle,
                Value = g.GroupId.ToString()
            }).ToList();
        }

        public List<SelectListItem> GetTeacher()
        {
            return _context.UsersRoles.Where(r => r.RoleId == 1)
                .Include(r => r.User).Select(u => new
                SelectListItem()
                {
                    Value = u.UserId.ToString(),
                    Text = u.User.UserName
                }).ToList();
        }

        public List<SelectListItem> GetLevels()
        {
            return _context.CourseLevels.Select(l => new SelectListItem()
            {
                Value = l.LevelId.ToString(),
                Text = l.LevelTitle
            }).ToList();
        }

        // public List<SelectListItem> GetPlan()
        // {
        //     return _context.CoursePlans.Select(p => new SelectListItem()
        //     {
        //         Value = p.PlanId.ToString(),
        //         Text = p.PlanLevel
        //     }).ToList();
        // }

        public List<SelectListItem> GetStatus()
        {
            return _context.CourseStatus.Select(s => new SelectListItem()
            {
                Value = s.StatusId.ToString(),
                Text = s.StatusTitle
            }).ToList();
        }

        public int AddCourse(Course course, IFormFile courseImage, IFormFile Demo)
        {
            course.CreateDate = DateTime.Now;

            course.CourseImageName = "/UserPanel/Defult.jpg";

            //check img
            if (courseImage != null && courseImage.IsImage())
            {
                //Image Name
                course.CourseImageName = CreateUniqCode.buildUniqCod() + Path.GetExtension(courseImage.FileName);

                //Image Path
                string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Course/MainImage", course.CourseImageName);

                //save
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    courseImage.CopyTo(stream);
                }
                //image resaiz
                var imageResizer = new ImageConvertor();
                string thumPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Course/Thumnail", course.CourseImageName);

                imageResizer.Resize(imagePath, thumPath, 500);

                if (Demo != null)
                {
                    //Image Name
                    course.DemoFileName = CreateUniqCode.buildUniqCod() + Path.GetExtension(Demo.FileName);
                    //Image Path
                    string demoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Course/Demo", course.DemoFileName);
                    using var stream = new FileStream(demoPath, FileMode.Create);
                    Demo.CopyTo(stream);
                }
            }
            _context.Add(course);
            _context.SaveChanges();
            return course.CourseId;
        }

        public ShowListForCourse GetListCourse(int pageId = 1, string filterTitle = "")
        {
            IQueryable<Course> result = _context.Course.Include(c => c.CourseEpisodes);
            if (!string.IsNullOrEmpty(filterTitle))
            {
                result = result.Where(u => u.CourseTitle.Contains(filterTitle));
            }
            int take = 8;
            int skip = (pageId - 1) * take;

            ShowListForCourse list = new ShowListForCourse
            {
                Course = result.OrderByDescending(u => u.CreateDate).Skip(skip).Take(take).ToList(),
                PageCount = result.Count() / take,
                EpisodeCount = result.Include(r => r.CourseEpisodes).Count()
            };
            if ((list.PageCount % 2) != 0)
            {
                list.PageCount++;
            }
            return list;
        }

        public Course GetCourseByCourseId(int courseId)
        {
            return _context.Course.Find(courseId);
        }

        public void UpdateCourse(Course course, IFormFile courseImage, IFormFile Demo)
        {
            course.UpdateDate = DateTime.Now;

            //check img
            if (courseImage != null && courseImage.IsImage())
            {
                if (course.CourseImageName != "Defult.jpg")
                {
                    string deletImgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Course/MainImage", course.CourseImageName);
                    if (File.Exists(deletImgPath))
                    {
                        File.Delete(deletImgPath);
                    }
                    string deletImgtumPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Course/Thumnail", course.CourseImageName);
                    if (File.Exists(deletImgtumPath))
                    {
                        File.Delete(deletImgtumPath);
                    }
                }
                //Image Name
                course.CourseImageName = CreateUniqCode.buildUniqCod() + Path.GetExtension(courseImage.FileName);

                //Image Path
                string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Course/MainImage", course.CourseImageName);

                //save
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    courseImage.CopyTo(stream);
                }
                //image resaiz
                var imageResizer = new ImageConvertor();
                string thumPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Course/Thumnail", course.CourseImageName);

                imageResizer.Resize(imagePath, thumPath, 600);

                if (Demo != null)
                {
                    if (course.DemoFileName != null)
                    {
                        string deletDemoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Course/Demo", course.DemoFileName);
                        if (File.Exists(deletDemoPath))
                        {
                            File.Delete(deletDemoPath);
                        }
                    }
                    //Image Name
                    course.DemoFileName = CreateUniqCode.buildUniqCod() + Path.GetExtension(Demo.FileName);

                    //Image Path
                    string demoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Course/Demo", course.DemoFileName);
                    using var stream = new FileStream(demoPath, FileMode.Create);
                    Demo.CopyTo(stream);
                }
            }
            _context.Course.Update(course);
            _context.SaveChanges();
        }

        public int AddCourseEpisode(CourseEpisode episode, IFormFile episodeFile)
        {
            episode.EpisodeFileName = episodeFile.FileName;
            string episodeFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Episode", episode.EpisodeFileName);
            using (var stream = new FileStream(episodeFilePath, FileMode.Create))
            {
                episodeFile.CopyTo(stream);
            }

            _context.CourseEpisode.Add(episode);
            _context.SaveChanges();
            return episode.EpisodeId;
        }

        public bool IsExistFile(string fileName)
        {
            string episodPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Episode", fileName);
            return File.Exists(episodPath);
        }

        public CourseEpisode GetEpisodeById(int episodeid)
        {
            return _context.CourseEpisode.Find(episodeid);
        }

        public void EditEpisode(CourseEpisode episode, IFormFile episodefile)
        {
            // check file delete
            if (episodefile != null)
            {
                string deletePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Episode", episode.EpisodeFileName);
                File.Delete(deletePath);
            }
            // insert file
            episode.EpisodeFileName = episodefile.FileName;
            string pathFile = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Episode", episode.EpisodeFileName);
            using (var stream = new FileStream(pathFile, FileMode.Create))
            {
                episodefile.CopyTo(stream);
            }
            _context.CourseEpisode.Update(episode);
            _context.SaveChanges();
        }

        public Tuple<List<ShowCourseItemViewModel>, int> GetCourse(int pageId = 1, int take = 0, string filter = "", string typeShow = "all", string orderBy = "date",
         int startPrice = 0, int endPrice = 0, List<int> selectedGroup = null)
        {
            if (take == 0)
            {
                take = 8;
            }

            IQueryable<Course> result = _context.Course;
            if (!string.IsNullOrEmpty(filter))
            {
                result = result.Where(c => c.CourseTitle.Contains(filter) || c.Tags.Contains(filter));
            }

            switch (typeShow)
            {
                case "all":
                    break;

                case "buy":
                    {
                        result = result.Where(c => c.CoursePrice != 0);
                        break;
                    }
                case "free":
                    {
                        result = result.Where(c => c.CoursePrice == 0);
                        break;
                    }
            }

            switch (orderBy)
            {
                case "date":
                    {
                        result = result.OrderByDescending(c => c.CreateDate);
                        break;
                    }
                case "updateDate":
                    {
                        result = result.OrderByDescending(c => c.UpdateDate);
                        break;
                    }
            }

            if (startPrice > 0)
            {
                result = result.Where(c => c.CoursePrice > startPrice);
            }

            if (endPrice > 0)
            {
                result = result.Where(c => c.CoursePrice < startPrice);
            }

            if (selectedGroup != null && selectedGroup.Any())
            {
                foreach (var groupId in selectedGroup)
                {
                    result = result.Where(c => c.GroupId == groupId || c.SubGroup == groupId);
                }
            }
            int skip = (pageId - 1) * take;
            var pageCount = result.Include(c => c.CourseEpisodes).Select(c => new ShowCourseItemViewModel()
            {
                CourseId = c.CourseId,
                Price = c.CoursePrice,
                Title = c.CourseTitle,
                ImageName = c.CourseImageName,
                UserCourse = c.UserCourses,
                TotalTime = new TimeSpan(c.CourseEpisodes.Sum(e => e.EpisodeTime.Ticks))
            }).Count() / take;
            if ((pageCount % 2) != 0)
            {
                pageCount++;
            }
            var query = result.Include(c => c.CourseEpisodes).Select(c => new ShowCourseItemViewModel()
            {
                CourseId = c.CourseId,
                Price = c.CoursePrice,
                Title = c.CourseTitle,
                ImageName = c.CourseImageName,
                UserCourse = c.UserCourses,
                //TotalTime = new TimeSpan(c.CourseEpisodes.Sum(e => e.EpisodeTime.Ticks)),
                CourseEpisodes = c.CourseEpisodes
            }).Skip(skip).Take(take).ToList();
            return Tuple.Create(query, pageCount);
        }

        public List<ShowCourseCreateQuestionViewModel> GetCourseCreateQuestion()
        {
            return _context.Course.Select(c => new ShowCourseCreateQuestionViewModel()
            {
                CourseId = c.CourseId,
                Title = c.CourseTitle,
            }).Take(8).ToList();
        }

        public Course GetCourseInfoByShowCourseDetailse(int courseId)
        {
            return _context.Course.Include(c => c.CourseEpisodes)
                .Include(c => c.CourseStatuses).Include(c => c.CourseLevels)
                .Include(c => c.User).Include(c => c.UserCourses)
                .FirstOrDefault(c => c.CourseId == courseId);
        }

        public void AddComment(CourseComment comment)
        {
            _context.CourseComment.Add(comment);
            _context.SaveChanges();
        }

        public Tuple<List<CourseComment>, int> GetListComment(int courseId, int pageId = 1)
        {
            int take = 10;
            int skip = (pageId - 1) * take;
            int pageCount = _context.CourseComment.Count(c => !c.IsDelete && c.CourseId == courseId) / take;
            if ((pageCount % 2) != 0)
            {
                pageCount++;
            }

            //show Commentlist
            return Tuple.Create(_context.CourseComment.Include(c => c.User)
                .Where(c => !c.IsDelete && c.CourseId == courseId).Skip(skip).Take(take)
                .OrderByDescending(c => c.CreateDate).ToList(), pageCount);
        }

        public List<Course> GetAllCourseTeacher(string teacherName)
        {
            var userId = _userService.GetUserIdByUsername(teacherName);
            return _context.Course.Include(c => c.CourseStatuses)
                .Include(c => c.CourseEpisodes).
                Where(c => c.TeacherId == userId).ToList();
        }

        public List<CourseEpisode> GetEpisodeByCourseId(int courseId)
        {
            var episode = _context.CourseEpisode.Include(ep => ep.Courses).Where(s => s.CourseId == courseId).ToList();
            return episode;
        }

        public List<CourseEpisode> GetListEpisode(int courseId)
        {
            return _context.CourseEpisode.Where(e => e.CourseId == courseId).ToList();
        }

        public List<ShowCourseItemViewModel> GetCoursePopular()
        {
            return _context.Course.Include(c => c.OrderDetails)
                .Where(d => d.OrderDetails.Any())
                .OrderByDescending(c => c.OrderDetails.Count).Take(8)
                .Select(c => new ShowCourseItemViewModel()
                {
                    CourseId = c.CourseId,
                    Title = c.CourseTitle,
                    ImageName = c.CourseImageName,
                    Price = c.CoursePrice,
                    CourseEpisodes = c.CourseEpisodes
                }).ToList();
        }

        public List<CourseGroup> GetAllCourseGroups()
        {
            return _context.CourseGroup.Include(g => g.CourseGroups).ToList();
        }

        public AdminCourseViewModel GetDeleteCourse(int pageId = 1, string filterCourseName = "")
        {
            IQueryable<Course> result = _context.Course.IgnoreQueryFilters().Where(u => u.IsDelete);
            //name
            if (!string.IsNullOrEmpty(filterCourseName))
            {
                result = result.Where(c => c.CourseTitle.Contains(filterCourseName));
            }
            int take = 3;
            int skip = (pageId - 1) * take;
            AdminCourseViewModel list = new AdminCourseViewModel
            {
                CurrentPage = pageId,
                Course = result.OrderBy(u => u.CreateDate).Skip(skip).Take(take).ToList(),
                PageCount = result.Count() / take
            };
            return list;
        }

        public InfoCourseViewModel GetCourseByListDelete(int courseId)
        {
            var course = GetCourseByCourseId(courseId);
            InfoCourseViewModel information = new InfoCourseViewModel();
            information.Title = course.CourseTitle;
            information.CreateDate = course.CreateDate;

            return information;
        }

        public void DeleteCourse(int courseId)
        {
            Course course = GetCourseByCourseId(courseId);
            course.IsDelete = true;
            UpdateCourse(course);
        }

        public void UpdateCourse(Course course)
        {
            _context.Course.Update(course);
            _context.SaveChanges();
        }

        public void AddCourseGroup(CourseGroup courseGroup)
        {
            _context.CourseGroup.Add(courseGroup);
            _context.SaveChanges();
        }

        public CourseGroup GetCourseGroupById(int groupId)
        {
            return _context.CourseGroup.Find(groupId);
        }

        public void UpdateCourseGroup(CourseGroup courseGroup)
        {
            _context.CourseGroup.Update(courseGroup);
            _context.SaveChanges();
        }

        #endregion Course

        #region Episode

        public bool AddEpisode(AddEpisodeTeacherViewModel episodeViewModel, string userName)
        {
            var course = GetCourseByCourseId(episodeViewModel.CourseId);

            var userId = _context.Users.FirstOrDefault(s => s.UserName == userName).UserId;

            if (course == null || course.TeacherId != userId)
            {
                return false;
            }

            var episode = new CourseEpisode()
            {
                CourseId = course.CourseId,
                IsFree = episodeViewModel.IsFree,
                EpisodeTitle = episodeViewModel.EpisodeTitle,
                EpisodeTime = episodeViewModel.EpisodeTime,
                EpisodeFileName = episodeViewModel.EpisodeFileName
            };

            _context.Add(episode);
            _context.SaveChanges();

            return true;
        }

        public CourseEpisode GetCourseEpisodeByEpisodeId(int episodeId)
        {
            return _context.CourseEpisode.Find(episodeId);
        }

        public void UpdateEpisode(CourseEpisode episode)
        {
            _context.CourseEpisode.Update(episode);
            _context.SaveChanges();
        }

        public void DeleteEpisode(int episodeId, IFormFile filename)
        {
            var episode = GetCourseEpisodeByEpisodeId(episodeId);
            if (episode.EpisodeFileName != null)
            {
                string deletePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Episode", episode.EpisodeFileName);
                System.IO.File.Delete(deletePath);
            }
            episode.IsDelete = true;
            UpdateEpisode(episode);
        }

        public InfoShowEpisodeDeleteViewModel GetEpisodeByDelete(int episodeId)
        {
            var episode = GetCourseEpisodeByEpisodeId(episodeId);
            InfoShowEpisodeDeleteViewModel info = new InfoShowEpisodeDeleteViewModel
            {
                EpisodeTitle = episode.EpisodeTitle,
                Imagename = episode.EpisodeFileName
            };
            return info;
        }

        #endregion Episode

        #region Vote

        public void AddsVote(int userId, int courseId, bool vote)
        {
            var UserVote = _context.CourseVote.FirstOrDefault(c => c.UserId == userId && c.CourseId == courseId);
            if (UserVote != null)
            {
                UserVote.Vote = vote;
            }
            else
            {
                UserVote = new CourseVote()
                {
                    CourseId = courseId,
                    UserId = userId,
                    Vote = vote
                };
                _context.Add(UserVote);
            }

            _context.SaveChanges();
        }

        public Tuple<int, int> GetCourseVotes(int courseId)
        {
            var votes = _context.CourseVote.Where(v => v.CourseId == courseId).Select(v => v.Vote).ToList();

            return Tuple.Create(votes.Count(c => c), votes.Count(c => !c));
        }

        public bool IsFree(int courseId)
        {
            return _context.Course.Where(c => c.CourseId == courseId).Select(c => c.CoursePrice).First() == 0;
        }

        #endregion Vote

        #region ShortLink For Course

        public string GenerateShortKey(int lenght = 4)
        {
            string key = Guid.NewGuid().ToString().Replace("-", "").Substring(0, lenght);

            while (_context.Course.Any(s => s.ShortKey == key))
            {
                key = Guid.NewGuid().ToString().Replace("-", "").Substring(0, lenght);
            }

            return key;
        }

        public Course GetShortKeyRedirect(string key)
        {
            return _context.Course.FirstOrDefault(c => c.ShortKey == key);
        }

        public int AddBanner(BannerInformation banner, IFormFile bannerImage)
        {
            //check img
            if (bannerImage != null && bannerImage.IsImage())
            {
                //Image Name
                banner.BannerImageName = CreateUniqCode.buildUniqCod() + Path.GetExtension(bannerImage.FileName);

                //Image Path
                string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Banner/MainImage", banner.BannerImageName);

                //save
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    bannerImage.CopyTo(stream);
                }
                //image resaiz
                //var imageResizer = new ImageConvertor();
                //string thumPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Course/Thumnail", course.CourseImageName);

                //imageResizer.Resize(imagePath, thumPath, 500);
            }
            _context.Add(banner);
            _context.SaveChanges();
            return banner.BannerId;
        }

        public BannerInformation GetBannerId(int bannerId)
        {
            return _context.BannerInformations.Find(bannerId);
        }

        public BannerInfoViewModel GetBannerForDelete(int bannerId)
        {
            var banner = GetBannerId(bannerId);
            BannerInfoViewModel information = new BannerInfoViewModel();
            information.ImageName = banner.BannerImageName;
            return information;
        }

        public List<BannerInformation> GetAllBannerForIndex()
        {
            return _context.BannerInformations.ToList();
        }

        public BannerInfoViewModel GetBannerToIndex()
        {
            return _context.BannerInformations.Select(b => new BannerInfoViewModel()
            {
                ImageName = b.BannerImageName,
            }).SingleOrDefault();
        }

        public void DeleteBanner(int bannerId)
        {
            var banner = GetBannerId(bannerId);
            banner.IsDelete = true;
            UpdateBanner(banner);
        }

        public void UpdateBanner(BannerInformation banner)
        {
            _context.BannerInformations.Update(banner);
            _context.SaveChanges();
        }

        public List<BannerInfoViewModel> GetBannerForIndex()
        {
            return _context.BannerInformations
                .Select(c => new BannerInfoViewModel()
                {
                    BannerId = c.BannerId,
                    IsDelete = false,
                    ImageName = c.BannerImageName
                })
                .Take(1).ToList();
        }

        #endregion ShortLink For Course
    }
}