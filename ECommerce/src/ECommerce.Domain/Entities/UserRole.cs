namespace ECommerce.Domain.Entities
{
    public enum UserRole
    {
        Admin,
        User
    }

    public static class UserRoles
    {
        public const string Admin = nameof(Admin);
        public const string User = nameof(User);
    }
}
