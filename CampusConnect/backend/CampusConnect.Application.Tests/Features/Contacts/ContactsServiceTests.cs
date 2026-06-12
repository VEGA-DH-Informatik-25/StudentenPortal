using CampusConnect.Application.Features.Contacts;
using CampusConnect.Domain.Entities;
using CampusConnect.Domain.Enums;
using CampusConnect.Domain.Interfaces;

namespace CampusConnect.Application.Tests.Features.Contacts;

public sealed class ContactsServiceTests
{
    [Fact]
    public async Task SearchAsync_ShouldFindUsersByCourseAndProfileDetails()
    {
        var currentUser = User("Alice", "alice@dhbw-loerrach.de", "TIF25A");
        var bob = User("Bob", "bob@dhbw-loerrach.de", "WWI25A", location: "Library", profileNote: "Looking for a project group");
        var clara = User("Clara", "clara@dhbw-loerrach.de", "TIF25B", phoneNumber: "+49 7621 123456");
        var service = new ContactsService(new FakeUserRepository(currentUser, bob, clara));

        var byNote = await service.SearchAsync(currentUser.Id, "project group");
        var byCourse = await service.SearchAsync(currentUser.Id, "TIF25B");

        var noteResult = Assert.Single(byNote);
        Assert.Equal(bob.Id, noteResult.Id);
        Assert.Equal("Library", noteResult.Location);
        Assert.DoesNotContain(byCourse, contact => contact.Id == currentUser.Id);
        Assert.Contains(byCourse, contact => contact.Id == clara.Id && contact.PhoneNumber == "+49 7621 123456");
    }

    [Fact]
    public async Task SearchAsync_ShouldRespectRequestedLimit()
    {
        var currentUser = User("Alice", "alice@dhbw-loerrach.de", "TIF25A");
        var service = new ContactsService(new FakeUserRepository(
            currentUser,
            User("Bob", "bob@dhbw-loerrach.de", "TIF25A"),
            User("Clara", "clara@dhbw-loerrach.de", "TIF25A"),
            User("David", "david@dhbw-loerrach.de", "TIF25A")));

        var results = await service.SearchAsync(currentUser.Id, "TIF25A", limit: 2);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchAsync_ShouldClampLimitToAtLeastOne()
    {
        var currentUser = User("Alice", "alice@dhbw-loerrach.de", "TIF25A");
        var service = new ContactsService(new FakeUserRepository(
            currentUser,
            User("Bob", "bob@dhbw-loerrach.de", "TIF25A"),
            User("Clara", "clara@dhbw-loerrach.de", "TIF25A")));

        var results = await service.SearchAsync(currentUser.Id, "TIF25A", limit: 0);

        Assert.Single(results);
    }

    private static User User(string displayName, string email, string course, string phoneNumber = "", string location = "", string profileNote = "") => new()
    {
        DisplayName = displayName,
        Email = email,
        StudyProgram = "Computer Science",
        Semester = 3,
        Course = course,
        PhoneNumber = phoneNumber,
        Location = location,
        ProfileNote = profileNote,
        Role = UserRole.Student
    };

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        private readonly List<User> _users = users.ToList();

        public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(_users);

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Email == email));

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _users.RemoveAll(user => user.Id == id);
            return Task.CompletedTask;
        }
    }
}
