using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Banner
{
   public class BannerInfoViewModel
    {
        public int BannerId { get; set; }
        public string ImageName { get; set; }
        public bool IsDelete { get; set; }
    }
}
