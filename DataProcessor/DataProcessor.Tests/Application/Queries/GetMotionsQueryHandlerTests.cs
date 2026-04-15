namespace DataProcessor.Tests.Application.Queries;

public class GetMotionsQueryHandlerTests
{
    private readonly IFixture _fixture;
    private readonly IMetricBaseRepository<Motion> _repository;
    private readonly GetMotionsQueryHandler _sut;

    public GetMotionsQueryHandlerTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _repository = Substitute.For<IMetricBaseRepository<Motion>>();
        _sut = new GetMotionsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithEntities_ReturnsSuccessWithMappedModels()
    {
        // Arrange
        var entities = _fixture.Build<Motion>()
            .With(x => x.RoomId, _fixture.Create<string>())
            .CreateMany(3)
            .ToList();

        var query = new GetMotionsQuery(
            _fixture.Create<DateTime>(),
            _fixture.Create<DateTime>(),
            _fixture.Create<string>());

        _repository
            .GetAllAsync(
                Arg.Any<string?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(entities);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(entities.Count, result.Value.Count);
        Assert.Equal(
            entities.Select(e => e.Id),
            result.Value.Select(m => m.Id));
        Assert.Equal(
            entities.Select(e => e.RoomId),
            result.Value.Select(m => m.RoomId));
        Assert.Equal(
            entities.Select(e => e.IsDetected),
            result.Value.Select(m => m.IsDetected));
        Assert.Equal(
            entities.Select(e => e.Timestamp),
            result.Value.Select(m => m.Timestamp));
    }

    [Fact]
    public async Task Handle_WithNoEntities_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var query = new GetMotionsQuery(null, null, null);

        _repository
            .GetAllAsync(
                Arg.Any<string?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Motion>());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }
}
