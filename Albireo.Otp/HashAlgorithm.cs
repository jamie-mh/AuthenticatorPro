namespace Albireo.Otp
{
    /// <summary>Available hashing algorithms.</summary>
    public enum HashAlgorithm : byte
    {
        /// <summary>Represents an unknown hashing algorithm.</summary>
        /// <remarks>Must not be supplied to OTP methods as it will raise an exception.</remarks>
        Unknown = 0,

        /// <summary>The MD5 hashing algorithm.</summary>
        Md5 = 1,

        /// <summary>The SHA1 hashing algorithm.</summary>
        Sha1 = 2,

        /// <summary>The SHA256 hashing algorithm.</summary>
        Sha256 = 3,

        /// <summary>The SHA512 hashing algorithm.</summary>
        Sha512 = 4
    }
}
