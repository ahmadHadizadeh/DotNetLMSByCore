using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Entities.User
{
    public class UserPlan
    {
        [Key]
        public int Up_Id { get; set; }

        public int UserId { get; set; }
        public int PlanId { get; set; }

        #region relation

        public Users User { get; set; }
        public Plan Plans { get; set; }

        #endregion relation
    }
}