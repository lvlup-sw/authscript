using Gateway.API.Contracts.Http;
using Gateway.API.Services.Http;

namespace Gateway.API.Tests.Services.Http;

public class TokenStrategyResolverTests
{
    [Test]
    public async Task Resolve_WhenStrategyCanHandle_ReturnsStrategy()
    {
        // Arrange
        var canHandleStrategy = new TestStrategy(canHandle: true);
        var cannotHandleStrategy = new TestStrategy(canHandle: false);
        var resolver = new TokenStrategyResolver([cannotHandleStrategy, canHandleStrategy]);

        // Act
        var result = resolver.Resolve();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsSameReferenceAs(canHandleStrategy);
    }

    [Test]
    public async Task Resolve_WhenNoStrategyCanHandle_ThrowsInvalidOperationException()
    {
        // Arrange
        var strategy1 = new TestStrategy(canHandle: false);
        var strategy2 = new TestStrategy(canHandle: false);
        var resolver = new TokenStrategyResolver([strategy1, strategy2]);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve());
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Resolve_WhenNoStrategiesRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var resolver = new TokenStrategyResolver([]);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve());
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Resolve_WhenMultipleCanHandle_ReturnsFirst()
    {
        // Arrange
        var first = new TestStrategy(canHandle: true, name: "first");
        var second = new TestStrategy(canHandle: true, name: "second");
        var resolver = new TokenStrategyResolver([first, second]);

        // Act
        var result = resolver.Resolve();

        // Assert
        await Assert.That(result).IsSameReferenceAs(first);
    }

    private sealed class TestStrategy : ITokenAcquisitionStrategy
    {
        private readonly bool _canHandle;
        public string Name { get; }

        public TestStrategy(bool canHandle, string name = "test")
        {
            _canHandle = canHandle;
            Name = name;
        }

        public bool CanHandle => _canHandle;

        public Task<string?> AcquireTokenAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("test-token");
    }
}
