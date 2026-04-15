namespace DataProcessor.Tests.Application.Queries;

public class GetAirQualitiesQueryHandlerTests
{
    private readonly IFixture _fixture;
    private readonly IMetricBaseRepository<AirQuality> _repository;
    private readonly GetAirQualitiesQueryHandler _sut;

    public GetAirQualitiesQueryHandlerTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _repository = Substitute.For<IMetricBaseRepository<AirQuality>>();
        _sut = new GetAirQualitiesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithEntities_ReturnsSuccessWithMappedModels()
    {
        // Arrange
        var entities = _fixture.Build<AirQuality>()
            .With(x => x.RoomId, _fixture.Create<string>())
            .CreateMany(3)
            .ToList();

        var query = new GetAirQualitiesQuery(
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
            entities.Select(e => e.Pm25),
            result.Value.Select(m => m.Pm25));
        Assert.Equal(
            entities.Select(e => e.Co2),
            result.Value.Select(m => m.Co2));
        Assert.Equal(
            entities.Select(e => e.Humidity),
            result.Value.Select(m => m.Humidity));
    }

    [Fact]
    public async Task Handle_WithNoEntities_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var query = new GetAirQualitiesQuery(null, null, null);

        _repository
            .GetAllAsync(
                Arg.Any<string?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }
}
