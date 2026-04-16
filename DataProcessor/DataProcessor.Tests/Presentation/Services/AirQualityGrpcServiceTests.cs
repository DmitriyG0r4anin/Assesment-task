namespace DataProcessor.Tests.Presentation.Services;

public class AirQualityGrpcServiceTests
{
    private readonly IFixture _fixture;
    private readonly IMediator _mediator;
    private readonly ILogger<AirQualityGrpcService> _logger;
    private readonly AirQualityGrpcService _sut;

    public AirQualityGrpcServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<AirQualityGrpcService>>();
        GrpcMappingConfig.RegisterMappings();
        _sut = new AirQualityGrpcService(_mediator, _logger);
    }

    private static ServerCallContext CreateServerCallContext()
    {
        var context = Substitute.For<ServerCallContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task GetAirQuality_WhenAirQualityIdIsEmpty_ReturnsValidationErrorResponse()
    {
        // Arrange
        var request = new GetAirQualityRequest { AirQualityId = string.Empty };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetAirQuality(request, context);

        // Assert
        Assert.Equal(GetAirQualityResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(400, response.Error.Code);
        _ = _mediator.DidNotReceive().Send(Arg.Any<GetAirQualityQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAirQuality_WhenAirQualityIdIsNull_ReturnsValidationErrorResponse()
    {
        // Arrange
        var request = new GetAirQualityRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetAirQuality(request, context);

        // Assert
        Assert.Equal(GetAirQualityResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(400, response.Error.Code);
        _ = _mediator.DidNotReceive().Send(Arg.Any<GetAirQualityQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAirQuality_WhenEntityExists_ReturnsDataResponse()
    {
        // Arrange
        var model = _fixture.Create<AirQualityModel>();
        Result<AirQualityModel> successResult = model;
        _mediator.Send(Arg.Any<GetAirQualityQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetAirQualityRequest { AirQualityId = model.Id };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetAirQuality(request, context);

        // Assert
        Assert.Equal(GetAirQualityResponse.ResultOneofCase.Data, response.ResultCase);
        Assert.Equal(model.Id, response.Data.Id);
    }

    [Fact]
    public async Task GetAirQuality_WhenEntityNotFound_ReturnsNotFoundErrorResponse()
    {
        // Arrange
        Result<AirQualityModel> failureResult = Error.NotFound;
        _mediator.Send(Arg.Any<GetAirQualityQuery>(), Arg.Any<CancellationToken>())
                 .Returns(failureResult);

        var request = new GetAirQualityRequest { AirQualityId = _fixture.Create<string>() };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetAirQuality(request, context);

        // Assert
        Assert.Equal(GetAirQualityResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(404, response.Error.Code);
    }

    [Fact]
    public async Task GetAirQuality_WhenIdIsValid_SendsCorrectQuery()
    {
        // Arrange
        var expectedId = _fixture.Create<string>();
        var model = _fixture.Create<AirQualityModel>();
        Result<AirQualityModel> successResult = model;
        _mediator.Send(Arg.Any<GetAirQualityQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetAirQualityRequest { AirQualityId = expectedId };
        var context = CreateServerCallContext();

        // Act
        await _sut.GetAirQuality(request, context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<GetAirQualityQuery>(q => q.AirQualityId == expectedId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAirQualities_WhenQuerySucceeds_ReturnsDataResponse()
    {
        // Arrange
        var models = _fixture.CreateMany<AirQualityModel>(3).ToList();
        Result<List<AirQualityModel>> successResult = models;
        _mediator.Send(Arg.Any<GetAirQualitiesQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetAirQualitiesRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetAirQualities(request, context);

        // Assert
        Assert.Equal(GetAirQualitiesResponse.ResultOneofCase.Data, response.ResultCase);
        Assert.Equal(models.Count, response.Data.AirQualities.Count);
    }

    [Fact]
    public async Task GetAirQualities_WhenResultIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        Result<List<AirQualityModel>> successResult = new List<AirQualityModel>();
        _mediator.Send(Arg.Any<GetAirQualitiesQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetAirQualitiesRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetAirQualities(request, context);

        // Assert
        Assert.Equal(GetAirQualitiesResponse.ResultOneofCase.Data, response.ResultCase);
        Assert.Empty(response.Data.AirQualities);
    }

    [Fact]
    public async Task GetAirQualities_WithRoomId_SendsQueryWithRoomId()
    {
        // Arrange
        var expectedRoomId = _fixture.Create<string>();
        var models = _fixture.CreateMany<AirQualityModel>(2).ToList();
        Result<List<AirQualityModel>> successResult = models;
        _mediator.Send(Arg.Any<GetAirQualitiesQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        // Setting RoomId makes HasRoomId == true (proto3 optional field presence)
        var request = new GetAirQualitiesRequest { RoomId = expectedRoomId };
        var context = CreateServerCallContext();

        // Act
        await _sut.GetAirQualities(request, context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<GetAirQualitiesQuery>(q => q.RoomId == expectedRoomId),
            Arg.Any<CancellationToken>());
    }
}
