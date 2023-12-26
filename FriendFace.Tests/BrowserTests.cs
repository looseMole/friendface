using System.Diagnostics;
using FriendFace.Data;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using Xunit.Abstractions;

namespace FriendFace.Tests;

public class BrowserTests: IDisposable
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
            .UseNpgsql("Host=cornelius.db.elephantsql.com;Port=5432;Database=xqpsjqpt;Username=xqpsjqpt;Password=5LMKmoVv1IdJar_Ka7uV4fi4Sht9PM8x")
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
        Thread.Sleep(3000); // adjust the delay as needed
        
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
        Thread.Sleep(3000); // adjust the delay as needed
        
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
        
        // Assert
        Thread.Sleep(2000);
        Assert.Equal("Home - FriendFace", _driver.Title);
    }
}