using System;

namespace loginpage.Utilities
{
    public static class IdHelper
    {
        public static string GetUniqIdValue()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}