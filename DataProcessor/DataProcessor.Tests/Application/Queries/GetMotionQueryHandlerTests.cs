namespace DataProcessor.Tests.Application.Queries;

public class GetMotionQueryHandlerTests
{
    private readonly IFixture _fixture;
    private readonly IMetricBaseRepository<Motion> _repository;
    private readonly GetMotionQueryHandler _sut;

    public GetMotionQueryHandlerTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _repository = Substitute.For<IMetricBaseRepository<Motion>>();
        _sut = new GetMotionQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenMotionExists_ReturnsSuccessWithCorrectlyMappedModel()
    {
        // Arrange
        var entity = _fixture.Build<Motion>()
            .With(x => x.RoomId, _fixture.Create<string>())
            .Create();

        var query = new GetMotionQuery(entity.Id);

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
        Assert.Equal(entity.IsDetected, result.Value.IsDetected);
    }

    [Fact]
    public async Task Handle_WhenMotionNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var query = new GetMotionQuery(_fixture.Create<string>());

        _repository
            .GetByIdAsync(query.MotionId, Arg.Any<CancellationToken>())
            .Returns((Motion?)null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Error.NotFound, result.Error);
    }
}
