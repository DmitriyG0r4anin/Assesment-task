namespace DataProcessor.Tests.Application.Queries;

public class GetRoomsQueryHandlerTests
{
    private readonly IFixture _fixture;
    private readonly IRoomRepository _repository;
    private readonly GetRoomsQueryHandler _sut;

    public GetRoomsQueryHandlerTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _repository = Substitute.For<IRoomRepository>();
        _sut = new GetRoomsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithRooms_ReturnsSuccessWithMappedModels()
    {
        // Arrange
        var entities = _fixture.Build<Room>()
            .With(x => x.Name, _fixture.Create<string>())
            .CreateMany(3)
            .ToList();

        var query = new GetRoomsQuery(null, null);

        _repository
            .GetAllAsync(Arg.Any<CancellationToken>())
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
            entities.Select(e => e.Name),
            result.Value.Select(m => m.Name));
    }

    [Fact]
    public async Task Handle_WithNoRooms_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var query = new GetRoomsQuery(null, null);

        _repository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Room>());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }
}
