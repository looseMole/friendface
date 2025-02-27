using System.Net;
using FriendFace.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FriendFace.Tests;

public class HomeControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private ApplicationDbContext _context;
    private IServiceScope _scope;

    public HomeControllerTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType ==
                             typeof(DbContextOptions<ApplicationDbContext>));

                    services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(
                            "Host=cornelius.db.elephantsql.com;Port=5432;Database=xqpsjqpt;Username=xqpsjqpt;Password=5LMKmoVv1IdJar_Ka7uV4fi4Sht9PM8x");
                    });
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        Initialize();
        SeedData().GetAwaiter().GetResult();
    }

    private void Initialize()
    {
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _context.Database.EnsureCreated();
    }

    private async Task SeedData()
    {
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            UserName = "jow",
            Email = "jow@example.com",
        };

        var result = await _scope.ServiceProvider.GetRequiredService<UserManager<User>>()
            .CreateAsync(user, "password");
        if (!result.Succeeded)
        {
            throw new Exception("Could not create user");
        }
    }

    public void Dispose()
    {
        // Remove all data from all tables after tests. Done with raw SQL for performance
        var tables = _context.Model.GetEntityTypes();

        // Begin a new transaction
        var transaction = _context.Database.BeginTransaction();
        foreach (var table in tables)
        {
            var tableName = table.GetTableName();
            _context.Database.ExecuteSqlRaw($"TRUNCATE TABLE \"{tableName}\" CASCADE;");
        }

        transaction.Commit();
        _context.Dispose();
    }

    [Fact]
    public async Task Post_CanBeCreated_WhenLoggedIn()
    {
        // Arrange
        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("uname", "jow"),
            new KeyValuePair<string, string>("psw", "password"),
        });
        try
        {
           await _client.PostAsync("/Login/Login", requestContent);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        // Act
        var postContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("content", "This is a post creation test"),
        });
        var postResponse = await _client.PostAsync("/Home/CreatePost", postContent);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
    }

    [Fact]
    public async Task Post_CanBeEdited_WhenLoggedIn()
    {
        // Arrange
        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("uname", "jow"),
            new KeyValuePair<string, string>("psw", "password"),
        });

        try
        {
            await _client.PostAsync("/Login/Login", requestContent);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        string contentString = "This is a post edit test";

        var postContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("content", contentString),
        });
        await _client.PostAsync("/Home/CreatePost", postContent);

        // Act
        // Discover post id
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Content == contentString);
        var editContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("PostId", post.Id.ToString()),
            new KeyValuePair<string, string>("Content", "This is an edited post"),
        });
        var editResponse = await _client.PostAsync("/Home/EditPost", editContent);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
    }

    [Fact]
    public async Task Post_CanBeDeleted_WhenOwnerLoggedIn()
    {
        // Arrange
        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("uname", "jow"),
            new KeyValuePair<string, string>("psw", "password"),
        });

        try
        {
            await _client.PostAsync("/Login/Login", requestContent);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        string contentString = "This is a post delete test";
        var postContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("content", contentString),
        });
        await _client.PostAsync("/Home/CreatePost", postContent);

        // Act
        // Discover post id
        var post = await _context.Posts.FirstOrDefaultAsync();
        var deleteResponse = await _client.GetAsync("/Home/DeletePost?postId=" + post.Id);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Post_CanBeLiked_WhenLoggedIn()
    {
        // Arrange
        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("uname", "jow"),
            new KeyValuePair<string, string>("psw", "password"),
        });

        try
        {
            await _client.PostAsync("/Login/Login", requestContent);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        string contentString = "This is a post like test";
        var postContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("content", contentString),
        });
        await _client.PostAsync("/Home/CreatePost", postContent);

        // Act
        // Discover post id
        var post = await _context.Posts.FirstOrDefaultAsync();
        var likeResponse = await _client.GetAsync("/Home/ToggleLikePost?postId=" + post.Id);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, likeResponse.StatusCode);
    }

    [Fact]
    public async Task Comment_CanBeCreated_WhenLoggedIn()
    {
        // Arrange
        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("uname", "jow"),
            new KeyValuePair<string, string>("psw", "password"),
        });
        try
        {
            await _client.PostAsync("/Login/Login", requestContent);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        string contentString = "This is a comment create test";
        var postContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("content", contentString),
        });
        await _client.PostAsync("/Home/CreatePost", postContent);

        // Act
        // Discover post id
        var post = await _context.Posts.FirstOrDefaultAsync();
        var commentContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("postId", post.Id.ToString()),
            new KeyValuePair<string, string>("content", "This is a test comment"),
        });
        var commentResponse = await _client.PostAsync("/Home/CreateComment", commentContent);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, commentResponse.StatusCode);
    }
}