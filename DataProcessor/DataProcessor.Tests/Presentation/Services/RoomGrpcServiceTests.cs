namespace DataProcessor.Tests.Presentation.Services;

public class RoomGrpcServiceTests
{
    private readonly IFixture _fixture;
    private readonly IMediator _mediator;
    private readonly ILogger<RoomGrpcService> _logger;
    private readonly RoomGrpcService _sut;

    public RoomGrpcServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<RoomGrpcService>>();
        GrpcMappingConfig.RegisterMappings();
        _sut = new RoomGrpcService(_mediator, _logger);
    }

    private static ServerCallContext CreateServerCallContext()
    {
        var context = Substitute.For<ServerCallContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task GetRoom_WhenRoomIdIsEmpty_ReturnsValidationErrorResponse()
    {
        // Arrange
        var request = new GetRoomRequest { RoomId = string.Empty };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetRoom(request, context);

        // Assert
        Assert.Equal(GetRoomResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(400, response.Error.Code);
        _ = _mediator.DidNotReceive().Send(Arg.Any<GetRoomQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRoom_WhenRoomIdIsNull_ReturnsValidationErrorResponse()
    {
        // Arrange
        var request = new GetRoomRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetRoom(request, context);

        // Assert
        Assert.Equal(GetRoomResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(400, response.Error.Code);
        _ = _mediator.DidNotReceive().Send(Arg.Any<GetRoomQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRoom_WhenEntityExists_ReturnsDataResponse()
    {
        // Arrange
        var model = _fixture.Create<RoomModel>();
        Result<RoomModel> successResult = model;
        _mediator.Send(Arg.Any<GetRoomQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        // Act
        var request = new GetRoomRequest { RoomId = model.Id };
        var context = CreateServerCallContext();

        var response = await _sut.GetRoom(request, context);

        // Assert
        Assert.Equal(GetRoomResponse.ResultOneofCase.Data, response.ResultCase);
        Assert.Equal(model.Id, response.Data.Id);
        Assert.Equal(model.Name, response.Data.Name);
    }

    [Fact]
    public async Task GetRoom_WhenEntityNotFound_ReturnsNotFoundErrorResponse()
    {
        // Arrange
        Result<RoomModel> failureResult = Error.NotFound;
        _mediator.Send(Arg.Any<GetRoomQuery>(), Arg.Any<CancellationToken>())
                 .Returns(failureResult);

        var request = new GetRoomRequest { RoomId = _fixture.Create<string>() };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetRoom(request, context);

        // Assert
        Assert.Equal(GetRoomResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(404, response.Error.Code);
    }

    [Fact]
    public async Task GetRoom_WhenIdIsValid_SendsCorrectQuery()
    {
        // Arrange
        var expectedId = _fixture.Create<string>();
        var model = _fixture.Create<RoomModel>();
        Result<RoomModel> successResult = model;
        _mediator.Send(Arg.Any<GetRoomQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetRoomRequest { RoomId = expectedId };
        var context = CreateServerCallContext();

        // Act
        await _sut.GetRoom(request, context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<GetRoomQuery>(q => q.RoomId == expectedId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRooms_WhenQuerySucceeds_ReturnsDataResponse()
    {
        // Arrange
        var models = _fixture.CreateMany<RoomModel>(3).ToList();
        Result<List<RoomModel>> successResult = models;
        _mediator.Send(Arg.Any<GetRoomsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetRoomsRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetRooms(request, context);

        // Assert
        Assert.Equal(GetRoomsResponse.ResultOneofCase.Data, response.ResultCase);
        Assert.Equal(models.Count, response.Data.Rooms.Count);
    }

    [Fact]
    public async Task GetRooms_WhenResultIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        Result<List<RoomModel>> successResult = new List<RoomModel>();
        _mediator.Send(Arg.Any<GetRoomsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetRoomsRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetRooms(request, context);

        // Assert
        Assert.Equal(GetRoomsResponse.ResultOneofCase.Data, response.ResultCase);
        Assert.Empty(response.Data.Rooms);
    }
}
