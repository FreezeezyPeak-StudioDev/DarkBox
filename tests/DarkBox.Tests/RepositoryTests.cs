using DarkBox.Core.Models;
using DarkBox.Core.Security;
using DarkBox.Data;
using Microsoft.Data.Sqlite;

namespace DarkBox.Tests;

public sealed class RepositoryTests : IDisposable
{
    private const string ValidPassword = "una contraseña maestra";
    private readonly string _directory;
    private readonly DarkBoxDatabase _database;
    private readonly UserRepository _users;
    private readonly VaultRepository _vault;

    public RepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "DarkBoxTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);

        _database = new DarkBoxDatabase(Path.Combine(_directory, "darkbox-test.db"));
        _database.Initialize();

        _users = new UserRepository(_database, new ProtectionParameters { Iterations = 10_000 });
        _vault = new VaultRepository(_database);
    }

    [Fact]
    public async Task CreateAndUnlock_RoundTrips()
    {
        var created = await _users.CreateAsync("jarvio", ValidPassword);

        Assert.True(created.IsSuccess);
        using var createdSession = created.Session!;

        using var unlockedSession = await _users.UnlockAsync("JARVIO", ValidPassword);

        Assert.NotNull(unlockedSession);
        Assert.Equal(createdSession.UserId, unlockedSession!.UserId);
        Assert.Equal("jarvio", unlockedSession.Username);
    }

    [Fact]
    public async Task DuplicateUser_Fails()
    {
        using var first = (await _users.CreateAsync("jarvio", ValidPassword)).Session!;
        var duplicate = await _users.CreateAsync("JARVIO", ValidPassword);

        Assert.False(duplicate.IsSuccess);
        Assert.NotNull(duplicate.Error);
    }

    [Fact]
    public async Task WrongPassword_ReturnsNull()
    {
        using var session = (await _users.CreateAsync("jarvio", ValidPassword)).Session!;

        var unlocked = await _users.UnlockAsync("jarvio", "esta contraseña es incorrecta");

        Assert.Null(unlocked);
    }

    [Fact]
    public async Task VaultItems_AreScopedPerUser()
    {
        using var alice = (await _users.CreateAsync("alice", ValidPassword)).Session!;
        using var bob = (await _users.CreateAsync("bob", ValidPassword)).Session!;

        var aliceItem = await _vault.AddAsync(
            alice,
            new VaultItemPayload("GitHub", "alice", "password-alice"));

        await _vault.AddAsync(
            bob,
            new VaultItemPayload("GitLab", "bob", "password-bob"));

        var aliceItems = await _vault.LoadAllAsync(alice);
        var bobItems = await _vault.LoadAllAsync(bob);

        Assert.Single(aliceItems);
        Assert.Equal("GitHub", aliceItems[0].Service);

        Assert.Single(bobItems);
        Assert.Equal("GitLab", bobItems[0].Service);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _vault.DeleteAsync(bob, aliceItem.Id));

        Assert.Single(await _vault.LoadAllAsync(alice));
    }

    [Fact]
    public async Task UpdateAndDelete_Work()
    {
        using var session = (await _users.CreateAsync("jarvio", ValidPassword)).Session!;

        var created = await _vault.AddAsync(
            session,
            new VaultItemPayload("GitHub", "jarvio", "password-vieja"));

        var updated = new VaultItemPayload(
            "GitHub",
            "jarvio",
            "password-nueva",
            "https://github.com",
            "Nota actualizada");

        await _vault.UpdateAsync(session, created.Id, updated);

        var items = await _vault.LoadAllAsync(session);
        var item = Assert.Single(items);

        Assert.Equal("password-nueva", item.Password);
        Assert.Equal("https://github.com", item.Url);
        Assert.Equal("Nota actualizada", item.Notes);

        await _vault.DeleteAsync(session, created.Id);

        Assert.Empty(await _vault.LoadAllAsync(session));
    }

    [Fact]
    public async Task InvalidPayload_IsRejected()
    {
        using var session = (await _users.CreateAsync("jarvio", ValidPassword)).Session!;

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _vault.AddAsync(session, new VaultItemPayload("", "jarvio", "password")));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _vault.AddAsync(session, new VaultItemPayload("GitHub", "jarvio", "")));
    }

    [Fact]
    public async Task HasAnyUser_ReflectsDatabaseState()
    {
        Assert.False(await _users.HasAnyUserAsync());

        using var session = (await _users.CreateAsync("jarvio", ValidPassword)).Session!;

        Assert.True(await _users.HasAnyUserAsync());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // La limpieza de archivos temporales no debe hacer fallar los tests.
        }
    }
}
