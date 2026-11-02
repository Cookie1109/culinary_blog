using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Users;
using NSubstitute;

namespace CulinaryBlog.UnitTests;

public sealed class AdminUserDirectoryTests
{
    [Theory]
    [InlineData(null, null, 1, 20, true)]
    [InlineData("name", "Admin", 1, 100, true)]
    [InlineData(null, "Author", 2, 1, true)]
    [InlineData(null, "Guest", 1, 20, false)]
    [InlineData(null, "admin", 1, 20, false)]
    [InlineData(null, "", 1, 20, false)]
    [InlineData(null, null, 0, 20, false)]
    [InlineData(null, null, 1, 0, false)]
    [InlineData(null, null, 1, 101, false)]
    [InlineData(null, null, int.MaxValue, 100, false)]
    public void ValidatesDirectoryInputs(string? search, string? role, int page, int pageSize, bool valid)
    {
        var validator = new ListAdminUsersQueryValidator();
        Assert.Equal(valid, validator.Validate(new ListAdminUsersQuery(search, role, page, pageSize)).IsValid);
        Assert.False(validator.Validate(new ListAdminUsersQuery(new string('a', 101), null)).IsValid);
    }

    [Fact]
    public async Task UnauthorizedApplicationRequestNeverReadsDirectory()
    {
        var directory = Substitute.For<IAdminUserDirectory>();
        directory.IsCurrentUserAdminAsync(default).Returns(false);
        var exception = await Assert.ThrowsAsync<ContentProblemException>(() =>
            new ListAdminUsersQueryHandler(directory).Handle(new ListAdminUsersQuery(null, null), default));
        Assert.Equal(ContentProblemKind.Forbidden, exception.Kind);
        await directory.DidNotReceiveWithAnyArgs().ListAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task AuthorizedQueryTrimsSearchAndPreservesPaginationAndCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var directory = Substitute.For<IAdminUserDirectory>();
        directory.IsCurrentUserAdminAsync(cancellation.Token).Returns(true);
        var expected = new PageEnvelope<AdminUserDto>([], new PageMeta(2, 50, 0, 1, false, true));
        directory.ListAsync("Tên", "Author", 2, 50, cancellation.Token).Returns(expected);
        var result = await new ListAdminUsersQueryHandler(directory).Handle(
            new ListAdminUsersQuery("  Tên  ", "Author", 2, 50), cancellation.Token);
        Assert.Same(expected, result);
    }
}
