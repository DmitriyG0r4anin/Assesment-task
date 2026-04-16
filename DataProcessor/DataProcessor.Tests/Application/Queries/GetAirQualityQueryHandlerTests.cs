namespace DataProcessor.Tests.Application.Queries;

public class GetAirQualityQueryHandlerTests
{
    private readonly IFixture _fixture;
    private readonly IMetricBaseRepository<AirQuality> _repository;
    private readonly GetAirQualityQueryHandler _sut;

    public GetAirQualityQueryHandlerTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _repository = Substitute.For<IMetricBaseRepository<AirQuality>>();
        _sut = new GetAirQualityQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenAirQualityExists_ReturnsSuccessWithCorrectlyMappedModel()
    {
        // Arrange
        var entity = _fixture.Build<AirQuality>()
            .With(x => x.RoomId, _fixture.Create<string>())
            .Create();

        var query = new GetAirQualityQuery(entity.Id);

        _repository
            .GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(entity.Id, result.Value.Id);
        Assert.Equal(entity.RoomId, result.Value.RoomId);
        Assert.Equal(entity.Timestamp, result.Value.Timestamp);
        Assert.Equal(entity.Pm25, result.Value.Pm25);
        Assert.Equal(entity.Co2, result.Value.Co2);
        Assert.Equal(entity.Humidity, result.Value.Humidity);
    }

    [Fact]
    public async Task Handle_WhenAirQualityNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var query = new GetAirQualityQuery(_fixture.Create<string>());

        _repository
            .GetByIdAsync(query.AirQualityId, Arg.Any<CancellationToken>())
            .Returns((AirQuality?)null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Error.NotFound, result.Error);
    }
}
