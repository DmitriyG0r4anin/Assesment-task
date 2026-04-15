namespace DataProcessor.Tests.Application.Queries;

public class GetEnergyQueryHandlerTests
{
    private readonly IFixture _fixture;
    private readonly IMetricBaseRepository<Energy> _repository;
    private readonly GetEnergyQueryHandler _sut;

    public GetEnergyQueryHandlerTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _repository = Substitute.For<IMetricBaseRepository<Energy>>();
        _sut = new GetEnergyQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenEnergyExists_ReturnsSuccessWithCorrectlyMappedModel()
    {
        // Arrange
        var entity = _fixture.Build<Energy>()
            .With(x => x.RoomId, _fixture.Create<string>())
            .Create();

        var query = new GetEnergyQuery(entity.Id);

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
        Assert.Equal(entity.Amount, result.Value.Amount);
    }

    [Fact]
    public async Task Handle_WhenEnergyNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var query = new GetEnergyQuery(_fixture.Create<string>());

        _repository
            .GetByIdAsync(query.EnergyId, Arg.Any<CancellationToken>())
            .Returns((Energy?)null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Error.NotFound, result.Error);
    }
}
