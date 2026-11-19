namespace KeyLens.Security;

public interface IUserDataProvider
{
    Guid CurrentUserId { get; }
}
