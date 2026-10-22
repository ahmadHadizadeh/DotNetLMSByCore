using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using DataLayer.Entities.Forum;
using DataLayer.Entities.User;

namespace Core.DTO
{
    public class UserQuestionViewModel
    {
        public Question Question { get; set; }
        public List<Answer> Answer { get; set; }
    }
    public class AnswerInfoForAnswerViewModel{
        
        
        public int QuestionId { get; set; }
        
        public int UserId { get; set; }
        public string UserName { get; set; }
       
        public string Body { get; set; }

        public string  ImageUser { get; set; }
        public DateTime CreateDate { get; set; }
    }

}
