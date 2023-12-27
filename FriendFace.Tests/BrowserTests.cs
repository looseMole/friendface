using System.Diagnostics;
using FriendFace.Data;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using Xunit.Abstractions;

namespace FriendFace.Tests;

[Collection("Browser Tests")]
public class BrowserTests : IDisposable
{
    private IWebDriver _driver;
    private Process _process;
    private readonly ITestOutputHelper _output;
    private ApplicationDbContext _dbContext;

    public BrowserTests(ITestOutputHelper output)
    {
        _output = output;
        // Setup ChromeDriver using WebDriverManager
        new DriverManager().SetUpDriver(new ChromeConfig());
        _driver = new ChromeDriver();

        // Get path for FriendFace.csproj
        string projectPath = System.IO.Path.GetFullPath("..\\..\\..\\..\\FriendFace\\FriendFace.csproj");
        _output.WriteLine(projectPath);

        // Start the application
        _process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project " + projectPath, // adjust the path to your project file
                UseShellExecute = false,
                CreateNoWindow = true,
                Environment =
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["ASPNETCORE_URLS"] = "http://localhost:5032"
                }
            }
        };

        Initialize();

        _process.Start();
    }

    private void Initialize()
    {
        // Connect to PostGreSQL Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=cornelius.db.elephantsql.com;Port=5432;Database=xqpsjqpt;Username=xqpsjqpt;Password=5LMKmoVv1IdJar_Ka7uV4fi4Sht9PM8x")
            .Options;

        _dbContext = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();

        // Stop the application
        _process.Kill();
        _process.Dispose();

        // Remove all data from all tables after tests. Done with raw SQL for performance
        var tables = _dbContext.Model.GetEntityTypes();

        // Begin a new transaction
        var transaction = _dbContext.Database.BeginTransaction();
        foreach (var table in tables)
        {
            var tableName = table.GetTableName();
            _dbContext.Database.ExecuteSqlRaw($"TRUNCATE TABLE \"{tableName}\" CASCADE;");
        }

        transaction.Commit();
        _dbContext.Dispose();
    }

    [Fact]
    public void FrontPage_CanBeViewed_WithChrome()
    {
        // Arrange
        // Wait for the application to start
        Thread.Sleep(5000); // adjust the delay as needed

        // Act
        _driver.Navigate().GoToUrl("http://localhost:5032/");

        // Assert
        Assert.Equal("Welcome to FriendFace - FriendFace", _driver.Title);
    }

    [Fact]
    public void User_CanRegister_WithChrome()
    {
        // Arrange
        // Wait for the application to start
        Thread.Sleep(5000); // adjust the delay as needed

        // Act
        _driver.Navigate().GoToUrl("http://localhost:5032/Login/Register");
        Thread.Sleep(1000);
        _driver.FindElement(By.Id("fname-input")).SendKeys("John");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("lname-input")).SendKeys("Doe");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("uname-input")).SendKeys("jow");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("email-input")).SendKeys("jowwy@email.com");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("psw-input")).SendKeys("password");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("register-btn")).Click();
        Thread.Sleep(2000);

        // Assert
        Assert.Equal("Home - FriendFace", _driver.Title);
    }

    [Fact]
    public void HappyPath_CanBeFollowed_WithChrome()
    {
        // Arrange
        // Wait for the application to start
        Thread.Sleep(3000); // adjust the delay as needed

        // Act
        // GoTo Front Page
        _driver.Navigate().GoToUrl("http://localhost:5032/");
        Thread.Sleep(2000);
        Assert.Equal("Welcome to FriendFace - FriendFace", _driver.Title); // Check if on front page

        // Register User 1
        _driver.FindElement(By.Id("loginPage-btn")).Click();
        Thread.Sleep(2000);
        _driver.FindElement(By.Id("register-link")).Click();
        Thread.Sleep(1000);
        _driver.FindElement(By.Id("fname-input")).SendKeys("John");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("lname-input")).SendKeys("Doe");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("uname-input")).SendKeys("jow");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("email-input")).SendKeys("jowwy@email.com");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("psw-input")).SendKeys("password");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("register-btn")).Click();
        Thread.Sleep(2000);
        Assert.Equal("Home - FriendFace", _driver.Title); // Check if on home page

        // Create post
        _driver.FindElement(By.Id("post-btn")).Click();
        Thread.Sleep(100);
        string contentString = "This is a post create test";
        _driver.FindElement(By.Id("postContent-publishField")).SendKeys(contentString);
        Thread.Sleep(100);
        _driver.FindElement(By.Id("postContent-button-publish")).Click();
        Thread.Sleep(2000);

        // Find post
        _driver.FindElement(By.Id("profile-feed-btn")).Click();
        Thread.Sleep(1000);
        var post = _driver.FindElement(By.ClassName("card"));
        Assert.Contains(contentString, post.Text); // Check if post is created properly

        // Edit post
        _driver.FindElement(By.Id("postMenuButton")).Click();
        Thread.Sleep(500);
        _driver.FindElement(By.ClassName("dropdown-item")).Click();
        Thread.Sleep(500);
        contentString = "(edit) " + contentString;
        var editField = _driver.FindElement(By.ClassName("form-control-plaintext"));
        editField.Clear();
        Thread.Sleep(100);
        editField.SendKeys(contentString);
        Thread.Sleep(100);
        _driver.FindElements(By.ClassName("btn-success"))[1]
            .Click(); // Click on second button with class btn-success: First is for publishing post, second (this one) is for editing post
        Thread.Sleep(2000);

        // Check if post is edited properly
        _driver.FindElement(By.Id("profile-feed-btn")).Click();
        Thread.Sleep(1000);
        post = _driver.FindElement(By.ClassName("card"));
        Assert.Contains(contentString, post.Text); // Check if post is edited properly

        // Log out
        _driver.Navigate().GoToUrl("http://localhost:5032/Login/Logout");
        Thread.Sleep(2000);
        Assert.Equal("Welcome to FriendFace - FriendFace", _driver.Title); // Check if on front page

        // Check if post is visible on front page
        post = _driver.FindElement(By.ClassName("card"));
        Assert.Contains(contentString, post.Text); // Check if post is edited properly

        // Register User 2
        _driver.Navigate().GoToUrl("http://localhost:5032/Login/Register");
        Thread.Sleep(1000);
        _driver.FindElement(By.Id("fname-input")).SendKeys("Jane");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("lname-input")).SendKeys("Doe");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("uname-input")).SendKeys("jaw");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("email-input")).SendKeys("jawwy@email.com");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("psw-input")).SendKeys("password");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("register-btn")).Click();
        Thread.Sleep(2000);
        Assert.Equal("Home - FriendFace", _driver.Title); // Check if on home page

        // Follow User 1
        var searchBar = _driver.FindElement(By.Name("search"));
        searchBar.SendKeys("jow");
        Thread.Sleep(100);
        searchBar.SendKeys(Keys.Enter);
        Thread.Sleep(2000);
        _driver.FindElement(By.ClassName("btn-outline-secondary")).Click();
        Thread.Sleep(2000);
        Assert.Equal("View Profile - FriendFace", _driver.Title); // Check if on home page

        // See followed user's post
        _driver.Navigate().GoToUrl("http://localhost:5032/");
        Thread.Sleep(2000);
        Assert.Equal("Home - FriendFace", _driver.Title); // Check if on home page
        post = _driver.FindElement(By.ClassName("card"));
        Assert.Contains(contentString, post.Text); // Check if followed user's post is visible

        // Like post
        _driver.FindElement(By.ClassName("fa-heart")).Click();
        Thread.Sleep(2000);

        // Check color of like button
        var likeButton = _driver.FindElement(By.ClassName("fa-heart"));
        Assert.Contains("red", likeButton.GetAttribute("style")); // Check if like button is red

        // Comment on post
        _driver.FindElement(By.ClassName("fa-comment")).Click();
        Thread.Sleep(500);
        var commentField = _driver.FindElement(By.ClassName("form-control-plaintext"));
        string commentString = "This is a comment";
        commentField.SendKeys(commentString);
        Thread.Sleep(100);
        _driver.FindElements(By.ClassName("btn-success"))[1]
            .Click(); // Click on second button with class btn-success: First is for publishing post, second (this one) should now be for commenting on post
        Thread.Sleep(2000);
        post = _driver.FindElement(By.ClassName("card"));
        Assert.Contains(commentString, post.Text); // Check if comment is visible

        // Log out
        _driver.Navigate().GoToUrl("http://localhost:5032/Login/Logout");
        Thread.Sleep(2000);
        Assert.Equal("Welcome to FriendFace - FriendFace", _driver.Title); // Check if on front page

        // Log in as User 1
        _driver.FindElement(By.Id("loginPage-btn")).Click();
        Thread.Sleep(2000);
        _driver.FindElement(By.Id("uname-input")).SendKeys("jow");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("psw-input")).SendKeys("password");
        Thread.Sleep(100);
        _driver.FindElement(By.Id("login-btn")).Click();
        Thread.Sleep(2000);
        Assert.Equal("Home - FriendFace", _driver.Title); // Check if on home page

        // Delete post
        _driver.FindElement(By.Id("profile-feed-btn")).Click();
        Thread.Sleep(1000);
        _driver.FindElement(By.Id("postMenuButton")).Click();
        Thread.Sleep(500);
        _driver.FindElements(By.ClassName("dropdown-item"))[1].Click();
        Thread.Sleep(2000);
        _driver.FindElement(By.Id("profile-feed-btn")).Click();
        Thread.Sleep(1000);
        Assert.DoesNotContain(contentString, _driver.PageSource); // Check if post is deleted);

        // Log out
        _driver.Navigate().GoToUrl("http://localhost:5032/Login/Logout");
        Thread.Sleep(2000);
        Assert.Equal("Welcome to FriendFace - FriendFace", _driver.Title); // Check if on front page
    }
}