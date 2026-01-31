namespace Gateway.API.Tests.Contracts.Http;

using Gateway.API.Contracts.Http;
using NSubstitute;

public class TokenAcquisitionStrategyTests
{
    [Test]
    public async Task ITokenAcquisitionStrategy_HasAcquireTokenAsyncMethod()
    {
        // Arrange
        var strategy = Substitute.For<ITokenAcquisitionStrategy>();
        strategy.AcquireTokenAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("test-token"));

        // Act
        var token = await strategy.AcquireTokenAsync();

        // Assert
        await Assert.That(token).IsEqualTo("test-token");
    }

    [Test]
    public async Task ITokenAcquisitionStrategy_HasCanHandleProperty()
    {
        // Arrange
        var strategy = Substitute.For<ITokenAcquisitionStrategy>();
        strategy.CanHandle.Returns(true);

        // Act
        var canHandle = strategy.CanHandle;

        // Assert
        await Assert.That(canHandle).IsTrue();
    }
}
