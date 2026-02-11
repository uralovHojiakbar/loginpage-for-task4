using System;

namespace loginpage.Utilities
{
    public static class IdHelper
    {
        public static string GetUniqIdValue()
        {
            // important: small helper returns a unique id string
            // note: using GUID to guarantee uniqueness across systems
            // nota bene: if you need a shorter id, consider hashing Guid.NewGuid()
            return Guid.NewGuid().ToString("N");
        }
    }
}