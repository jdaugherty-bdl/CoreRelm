using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Enums
{
    public class SecurityEnums
    {
        public enum SqlSecurityLevel
        {
            None,
            INVOKER,
            DEFINER
        }

        public enum AccountStatus
        {
            Inactive,
            Active,
            Suspended,
            Closed
        }

        public enum PasswordStrength
        {
            Weak,
            Medium,
            Strong,
            VeryStrong
        }

        public enum UserRole
        {
            Guest,
            User,
            Moderator,
            Administrator,
            SuperAdministrator
        }

        public enum AuthenticationMethod
        {
            Password,
            OAuth2,
            SAML,
            MultiFactor
        }

        public enum EncryptionAlgorithm
        {
            AES256,
            RSA2048,
            ChaCha20
        }

        public enum HashingAlgorithm
        {
            SHA256,
            SHA512,
            Bcrypt
        }

        public enum AccessLevel
        {
            None,
            Read,
            Write,
            Admin
        }
    }
}
