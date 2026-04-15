namespace DataProcessor.Tests.Application.Queries;

public class GetRoomQueryHandlerTests
{
    private readonly IFixture _fixture;
    private readonly IRoomRepository _repository;
    private readonly GetRoomQueryHandler _sut;

    public GetRoomQueryHandlerTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _repository = Substitute.For<IRoomRepository>();
        _sut = new GetRoomQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenRoomExists_ReturnsSuccessWithCorrectlyMappedModel()
    {
        // Arrange
        var entity = _fixture.Build<Room>()
            .With(x => x.Name, _fixture.Create<string>())
            .Create();

        var query = new GetRoomQuery(entity.Id);

        _repository
            .GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(entity.Id, result.Value.Id);
        Assert.Equal(entity.Name, result.Value.Name);
    }

    [Fact]
    public async Task Handle_WhenRoomNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var query = new GetRoomQuery(_fixture.Create<string>());

        _repository
            .GetByIdAsync(query.RoomId, Arg.Any<CancellationToken>())
            .Returns((Room?)null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Error.NotFound, result.Error);
    }
}
