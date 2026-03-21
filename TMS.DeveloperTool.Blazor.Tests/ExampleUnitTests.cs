namespace TMS.DeveloperTool.Blazor.Tests;

public class ExampleUnitTests
{
    [Fact]
    public void Example_SimpleTest_ShouldPass()
    {
        // Arrange
        var expected = 2;

        // Act
        var result = 1 + 1;

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(2, 2, 4)]
    [InlineData(0, 5, 5)]
    public void Example_ParameterizedTest_ShouldCalculateCorrectly(int a, int b, int expected)
    {
        // Act
        var result = a + b;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Example_TestWithMock_ShouldWork()
    {
        // Arrange
        var mockService = new Mock<IExampleService>();
        mockService.Setup(x => x.GetValue()).Returns(42);

        // Act
        var result = mockService.Object.GetValue();

        // Assert
        result.Should().Be(42);
        mockService.Verify(x => x.GetValue(), Times.Once);
    }
}

// Example interface for mocking
public interface IExampleService
{
    int GetValue();
}
