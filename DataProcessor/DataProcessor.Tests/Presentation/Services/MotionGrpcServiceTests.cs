namespace DataProcessor.Tests.Presentation.Services;

public class MotionGrpcServiceTests
{
    private readonly IFixture _fixture;
    private readonly IMediator _mediator;
    private readonly ILogger<MotionGrpcService> _logger;
    private readonly MotionGrpcService _sut;

    public MotionGrpcServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<MotionGrpcService>>();
        GrpcMappingConfig.RegisterMappings();
        _sut = new MotionGrpcService(_mediator, _logger);
    }

    private static ServerCallContext CreateServerCallContext()
    {
        var context = Substitute.For<ServerCallContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task GetMotion_WhenMotionIdIsEmpty_ReturnsValidationErrorResponse()
    {
        // Arrange
        var request = new GetMotionRequest { MotionId = string.Empty };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetMotion(request, context);

        // Assert
        Assert.Equal(GetMotionResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(400, response.Error.Code);
        _ = _mediator.DidNotReceive().Send(Arg.Any<GetMotionQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMotion_WhenMotionIdIsNull_ReturnsValidationErrorResponse()
    {
        // Arrange
        var request = new GetMotionRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetMotion(request, context);

        // Assert
        Assert.Equal(GetMotionResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(400, response.Error.Code);
        _ = _mediator.DidNotReceive().Send(Arg.Any<GetMotionQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMotion_WhenEntityExists_ReturnsDataResponse()
    {
        // Arrange
        var model = _fixture.Create<MotionModel>();
        Result<MotionModel> successResult = model;
        _mediator.Send(Arg.Any<GetMotionQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetMotionRequest { MotionId = model.Id };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetMotion(request, context);

        // Assert
        Assert.Equal(GetMotionResponse.ResultOneofCase.Data, response.ResultCase);
        Assert.Equal(model.Id, response.Data.Id);
    }

    [Fact]
    public async Task GetMotion_WhenEntityNotFound_ReturnsNotFoundErrorResponse()
    {
        // Arrange
        Result<MotionModel> failureResult = Error.NotFound;
        _mediator.Send(Arg.Any<GetMotionQuery>(), Arg.Any<CancellationToken>())
                 .Returns(failureResult);

        var request = new GetMotionRequest { MotionId = _fixture.Create<string>() };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetMotion(request, context);

        // Assert
        Assert.Equal(GetMotionResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(404, response.Error.Code);
    }

    [Fact]
    public async Task GetMotion_WhenIdIsValid_SendsCorrectQuery()
    {
        // Arrange
        var expectedId = _fixture.Create<string>();
        var model = _fixture.Create<MotionModel>();
        Result<MotionModel> successResult = model;
        _mediator.Send(Arg.Any<GetMotionQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetMotionRequest { MotionId = expectedId };
        var context = CreateServerCallContext();

        // Act
        await _sut.GetMotion(request, context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<GetMotionQuery>(q => q.MotionId == expectedId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMotions_WhenMediatorReturnsSuccess_SendsQueryAndReturnsResponse()
    {
        // Arrange
        var models = _fixture.CreateMany<MotionModel>(3).ToList();
        Result<List<MotionModel>> successResult = models;
        _mediator.Send(Arg.Any<GetMotionsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetMotionsRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetMotions(request, context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Any<GetMotionsQuery>(),
            Arg.Any<CancellationToken>());

        Assert.NotNull(response);
    }
}
