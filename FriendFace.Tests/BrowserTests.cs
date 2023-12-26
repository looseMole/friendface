using System.Diagnostics;
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
                RedirectStandardOutput = true, // TODO: redirect stdout
                RedirectStandardError = true, // TODO: redirect stderr
                CreateNoWindow = true,
                Environment = {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["ASPNETCORE_URLS"] = "http://localhost:5032"
                }
            }
        };
        _process.Start();
    }
    
    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();

        // Stop the application
        _output.WriteLine(_process.StandardOutput.ReadToEnd());
        _output.WriteLine(_process.StandardError.ReadToEnd());
        _process.Kill();
        _process.Dispose();
    }
    
    [Fact]
    public void FrontPage_CanBeViewed_WithChrome()
    {
        // Arrange
        // Wait for the application to start
        Thread.Sleep(10000); // adjust the delay as needed
        
        // Act
        _driver.Navigate().GoToUrl("http://localhost:5032/");

        // Assert
        Assert.Equal("Welcome to FriendFace - FriendFace", _driver.Title);
    }
}