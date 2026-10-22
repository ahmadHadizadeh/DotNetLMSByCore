using System;

namespace Core.Genarate
{
    public class CreateUniqCode
    {
        public static string buildUniqCod()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

    }
}
