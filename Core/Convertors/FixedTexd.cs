using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Convertors
{
   public class FixedTexd
    {
        public static string FixedEmail(string email)
        {
            return email.Trim().ToLower();
        }
    }
}
