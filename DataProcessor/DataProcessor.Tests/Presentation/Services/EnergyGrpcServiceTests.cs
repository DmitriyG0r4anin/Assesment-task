namespace DataProcessor.Tests.Presentation.Services;

public class EnergyGrpcServiceTests
{
    private readonly IFixture _fixture;
    private readonly IMediator _mediator;
    private readonly ILogger<EnergyGrpcService> _logger;
    private readonly EnergyGrpcService _sut;

    public EnergyGrpcServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<EnergyGrpcService>>();
        GrpcMappingConfig.RegisterMappings();
        _sut = new EnergyGrpcService(_mediator, _logger);
    }

    private static ServerCallContext CreateServerCallContext()
    {
        var context = Substitute.For<ServerCallContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task GetEnergy_WhenEnergyIdIsEmpty_ReturnsValidationErrorResponse()
    {
        // Arrange
        var request = new GetEnergyRequest { EnergyId = string.Empty };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetEnergy(request, context);

        // Assert
        Assert.Equal(GetEnergyResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(400, response.Error.Code);
        _ = _mediator.DidNotReceive().Send(Arg.Any<GetEnergyQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEnergy_WhenEnergyIdIsNull_ReturnsValidationErrorResponse()
    {
        // Arrange
        var request = new GetEnergyRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetEnergy(request, context);

        // Assert
        Assert.Equal(GetEnergyResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(400, response.Error.Code);
        _ = _mediator.DidNotReceive().Send(Arg.Any<GetEnergyQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEnergy_WhenEntityExists_ReturnsDataResponse()
    {
        // Arrange
        var model = _fixture.Create<EnergyModel>();
        Result<EnergyModel> successResult = model;
        _mediator.Send(Arg.Any<GetEnergyQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetEnergyRequest { EnergyId = model.Id };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetEnergy(request, context);

        // Assert
        Assert.Equal(GetEnergyResponse.ResultOneofCase.Data, response.ResultCase);
        Assert.Equal(model.Id, response.Data.Id);
    }

    [Fact]
    public async Task GetEnergy_WhenEntityNotFound_ReturnsNotFoundErrorResponse()
    {
        // Arrange
        Result<EnergyModel> failureResult = Error.NotFound;
        _mediator.Send(Arg.Any<GetEnergyQuery>(), Arg.Any<CancellationToken>())
                 .Returns(failureResult);

        var request = new GetEnergyRequest { EnergyId = _fixture.Create<string>() };
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetEnergy(request, context);

        // Assert
        Assert.Equal(GetEnergyResponse.ResultOneofCase.Error, response.ResultCase);
        Assert.Equal(404, response.Error.Code);
    }

    [Fact]
    public async Task GetEnergy_WhenIdIsValid_SendsCorrectQuery()
    {
        // Arrange
        var expectedId = _fixture.Create<string>();
        var model = _fixture.Create<EnergyModel>();
        Result<EnergyModel> successResult = model;
        _mediator.Send(Arg.Any<GetEnergyQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetEnergyRequest { EnergyId = expectedId };
        var context = CreateServerCallContext();

        // Act
        await _sut.GetEnergy(request, context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<GetEnergyQuery>(q => q.EnergyId == expectedId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEnergies_WhenQuerySucceeds_ReturnsDataResponse()
    {
        // Arrange
        var models = _fixture.CreateMany<EnergyModel>(3).ToList();
        Result<List<EnergyModel>> successResult = models;
        _mediator.Send(Arg.Any<GetEnergiesQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetEnergiesRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetEnergies(request, context);

        // Assert
        Assert.Equal(GetEnergiesResponse.ResultOneofCase.Data, response.ResultCase);
        Assert.Equal(models.Count, response.Data.Energies.Count);
    }

    [Fact]
    public async Task GetEnergies_WhenResultIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        Result<List<EnergyModel>> successResult = new List<EnergyModel>();
        _mediator.Send(Arg.Any<GetEnergiesQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        var request = new GetEnergiesRequest();
        var context = CreateServerCallContext();

        // Act
        var response = await _sut.GetEnergies(request, context);

        // Arrange
        Assert.Equal(GetEnergiesResponse.ResultOneofCase.Data, response.ResultCase);
        Assert.Empty(response.Data.Energies);
    }

    [Fact]
    public async Task GetEnergies_WithRoomId_SendsQueryWithRoomId()
    {
        // Arrange
        var expectedRoomId = _fixture.Create<string>();
        var models = _fixture.CreateMany<EnergyModel>(2).ToList();
        Result<List<EnergyModel>> successResult = models;
        _mediator.Send(Arg.Any<GetEnergiesQuery>(), Arg.Any<CancellationToken>())
                 .Returns(successResult);

        // Setting RoomId makes HasRoomId == true (proto3 optional field presence)
        var request = new GetEnergiesRequest { RoomId = expectedRoomId };
        var context = CreateServerCallContext();

        // Act
        await _sut.GetEnergies(request, context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<GetEnergiesQuery>(q => q.RoomId == expectedRoomId),
            Arg.Any<CancellationToken>());
    }
}
