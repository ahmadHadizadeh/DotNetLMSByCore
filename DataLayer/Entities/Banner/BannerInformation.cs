using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Entities.Banner
{
    public class BannerInformation
    {
        [Key]
        public int BannerId { get; set; }

        public string BannerImageName { get; set; }
        public bool IsDelete { get; set; }
    }
}