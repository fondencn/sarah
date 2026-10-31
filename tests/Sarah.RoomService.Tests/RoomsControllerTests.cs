using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sarah.RoomService.WebApi.Controllers;
using Sarah.RoomService.WebApi.Data;
using Sarah.RoomService.WebApi.Data.Entities;
using Sarah.RoomService.WebApi.Data.Repositories;
using Sarah.RoomService.WebApi.DTOs;

namespace Sarah.RoomService.Tests;

public class RoomsControllerTests
{
    [Fact]
    public async Task GetAllReturnsRoomsInManualOrderRatherThanAlphabetically()
    {
        await using var dbContext = CreateDbContext(
            new RoomEntity { Id = 1, Name = "Zebra room", SortOrder = 0 },
            new RoomEntity { Id = 2, Name = "Alpha room", SortOrder = 1 });
        var controller = CreateController(dbContext);

        var result = await controller.GetAll(CancellationToken.None);

        var rooms = Assert.IsType<OkObjectResult>(result).Value as IEnumerable<RoomDto>;
        Assert.NotNull(rooms);
        Assert.Equal(new long[] { 1, 2 }, rooms.Select(room => room.Id));
    }

    [Fact]
    public async Task UpdateOrderPersistsFullRequestedOrder()
    {
        await using var dbContext = CreateDbContext(
            new RoomEntity { Id = 1, Name = "One", SortOrder = 0 },
            new RoomEntity { Id = 2, Name = "Two", SortOrder = 1 },
            new RoomEntity { Id = 3, Name = "Three", SortOrder = 2 });
        var controller = CreateController(dbContext);

        var result = await controller.UpdateOrder([3, 1, 2], CancellationToken.None);
        var orderedResult = await controller.GetAll(CancellationToken.None);
        var rooms = Assert.IsType<OkObjectResult>(orderedResult).Value as IEnumerable<RoomDto>;

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(rooms);
        Assert.Equal(new long[] { 3, 1, 2 }, rooms.Select(room => room.Id));
    }

    [Fact]
    public async Task LastSuccessfulOrderWriteWins()
    {
        await using var dbContext = CreateDbContext(
            new RoomEntity { Id = 1, Name = "One", SortOrder = 0 },
            new RoomEntity { Id = 2, Name = "Two", SortOrder = 1 });
        var controller = CreateController(dbContext);

        await controller.UpdateOrder([2, 1], CancellationToken.None);
        await controller.UpdateOrder([1, 2], CancellationToken.None);
        var orderedResult = await controller.GetAll(CancellationToken.None);
        var rooms = Assert.IsType<OkObjectResult>(orderedResult).Value as IEnumerable<RoomDto>;

        Assert.NotNull(rooms);
        Assert.Equal(new long[] { 1, 2 }, rooms.Select(room => room.Id));
    }

    [Fact]
    public async Task UpdateOrderAcceptsEmptyRoomList()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.UpdateOrder([], CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateOrderAcceptsSingleRoom()
    {
        await using var dbContext = CreateDbContext(new RoomEntity { Id = 9, Name = "Only room", SortOrder = 0 });
        var controller = CreateController(dbContext);

        var result = await controller.UpdateOrder([9], CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, await dbContext.Rooms.Select(room => room.SortOrder).SingleAsync());
    }

    [Theory]
    [InlineData(new long[] { 1, 1 })]
    [InlineData(new long[] { 1 })]
    [InlineData(new long[] { 1, 99 })]
    public async Task UpdateOrderRejectsDuplicatesMissingOrUnknownRooms(long[] ids)
    {
        await using var dbContext = CreateDbContext(
            new RoomEntity { Id = 1, Name = "One", SortOrder = 0 },
            new RoomEntity { Id = 2, Name = "Two", SortOrder = 1 });
        var controller = CreateController(dbContext);

        var result = await controller.UpdateOrder(ids, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(new[] { 0, 1 }, await dbContext.Rooms.OrderBy(room => room.Id).Select(room => room.SortOrder).ToArrayAsync());
    }

    private static ApplicationDbContext CreateDbContext(params RoomEntity[] rooms)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ApplicationDbContext(options);
        dbContext.Rooms.AddRange(rooms);
        dbContext.SaveChanges();
        return dbContext;
    }

    private static RoomsController CreateController(ApplicationDbContext dbContext)
    {
        return new RoomsController(
            new Repository<RoomEntity>(dbContext),
            dbContext,
            NullLogger<RoomsController>.Instance);
    }
}
