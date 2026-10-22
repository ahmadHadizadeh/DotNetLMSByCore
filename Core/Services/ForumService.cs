 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.DTO;
using Core.Services.Interfaces;
using DataLayer.Context;
 using DataLayer.Entities.Forum;
 using Microsoft.EntityFrameworkCore;


namespace Core.Services
{
  public  class ForumService: IForumService
    {
        private AsanwebContext _context;

        public ForumService(AsanwebContext context)
        {
            _context = context;
        }

        #region Question

        public int AddQuestion(Question question)
        {
            question.CreateDate = DateTime.Now;
            question.ModifiedDate = DateTime.Now;
            _context.Add(question);
            _context.SaveChanges();
            return question.QuestionId;
        }
        public IEnumerable<Question> GetQuestions(int? courseId, string filter = "")
        {
            IQueryable<Question>result=_context.Question.Where(q=>EF.Functions.Like(q.Title,$"%{filter}%"));
            if (courseId != null)
            {
                result=result.Where(q=>q.CourseId==courseId);
            }
            return result.Include(q=>q.Courses).Include(q=>q.User).ToList();
        }
        public UserQuestionViewModel GetQuestionUser(int questionId)
        {
            var question = new UserQuestionViewModel();
           
            question.Question = _context.Question.Include(q => q.User)
                .FirstOrDefault(q => q.QuestionId == questionId);
            question.Answer = _context.Answers.Where(a => a.QuestionId == questionId).Include(a=>a.User).ToList();
            return question;
        }
        
        public Question GetQuestionByQuestionId(int id)
        {
            return _context.Question.Find(id);
        }
        public bool IsExistQuestionId(int id)
        {
           return _context.Question.Any(q=>q.QuestionId==id);
        }

        #endregion

        #region Answer
        public void AddAnswer(Answer answer)
        {
            _context.Answers.Include(a => a.User);
            _context.Answers.Add(answer);
            _context.SaveChanges();
        }

        public void ChangeIsTrueAnswer(int questionId, int answerId)
        {
            var answers = _context.Answers.Where(a => a.QuestionId == questionId);
            foreach (var ans in answers)
            {
                ans.IsTrue = false;
                if (ans.AnswerId == answerId)
                {
                    ans.IsTrue = true;
                }
            }
            _context.UpdateRange(answers);
            _context.SaveChanges();
        }

      



        #endregion
    }
}
