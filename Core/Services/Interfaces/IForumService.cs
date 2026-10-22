using System;
using System.Collections.Generic;
using System.Text;
using Core.DTO;
using DataLayer.Entities.Forum;

namespace Core.Services.Interfaces
{
    public interface IForumService
    {
        #region Question
        IEnumerable<Question>GetQuestions(int?courseId,string filter="");
        int AddQuestion(Question question);
        UserQuestionViewModel GetQuestionUser(int questionId);
        Question GetQuestionByQuestionId(int id);
        bool IsExistQuestionId(int id);
        #endregion

        #region Answer


        void AddAnswer(Answer answer);
        void ChangeIsTrueAnswer(int questionId, int answerId);

        #endregion
    }
}
