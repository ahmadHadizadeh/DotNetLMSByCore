using DataLayer.Entities.User;
using System;
using System.Collections.Generic;

namespace Core.DTO
{
    public class ContactUsViewModel
    {

        public List<ContactUs> Contact { get; set; }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
