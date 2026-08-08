namespace Application.Common.Heplers
{
    public static class PasswordHepler
    {
        public static string PasswordHash(this string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool ValidPassword(this string pwd, string passwordHashed)
        {
            return BCrypt.Net.BCrypt.Verify(pwd, passwordHashed);
        }
    }
}
