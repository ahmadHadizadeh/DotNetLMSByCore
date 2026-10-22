using System;
using System.ComponentModel.DataAnnotations;

namespace Core.DTO
{
    public class ChargeWalletviewModel
    {
        [Display(Name = "مبلغ")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public int Amount { get; set; }

       

      
    }

    public class WalletInformationViewModel
    {
        public int Amount { get; set; }
        public int TypeId { get; set; }
        public string Description { get; set; }
        public DateTime CreateDate { get; set; }
        public int WalletId { get; set; }
    }
}
