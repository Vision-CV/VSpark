namespace VSpark.Models.Auth.Permissions;

[Flags]
public enum UserPermissions
{
    MetricsRead = 1 << 1,
    MetricsReview = 1 << 2,
    MetricsAdmin = 1 << 3,
    ServiceAdmin = 1 << 4
}
