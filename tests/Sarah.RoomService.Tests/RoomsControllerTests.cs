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
    public async Task GetAllExposesSortOrder()
    {
        await using var dbContext = CreateDbContext(new RoomEntity { Id = 1, Name = "One", SortOrder = 4 });
        var controller = CreateController(dbContext);

        var result = await controller.GetAll(CancellationToken.None);

        var rooms = Assert.IsType<OkObjectResult>(result).Value as IEnumerable<RoomDto>;
        Assert.Equal(4, Assert.Single(rooms!).SortOrder);
    }

    [Fact]
    public async Task UpdatePersistsSortOrderAndAffectsListOrder()
    {
        await using var dbContext = CreateDbContext(
            new RoomEntity { Id = 1, Name = "One", SortOrder = 0 },
            new RoomEntity { Id = 2, Name = "Two", SortOrder = 1 });
        var controller = CreateController(dbContext);

        var result = await controller.Update(1, new RoomDto { Id = 1, Name = "One", SortOrder = 5 }, CancellationToken.None);
        var listResult = await controller.GetAll(CancellationToken.None);
        var rooms = Assert.IsType<OkObjectResult>(listResult).Value as IEnumerable<RoomDto>;

        Assert.Equal(5, ((RoomDto)Assert.IsType<OkObjectResult>(result).Value!).SortOrder);
        Assert.Equal(new long[] { 2, 1 }, rooms!.Select(room => room.Id));
    }

    [Fact]
    public async Task CreateAppendsRoomAtEndOfOrder()
    {
        await using var dbContext = CreateDbContext(new RoomEntity { Id = 1, Name = "One", SortOrder = 3 });
        var controller = CreateController(dbContext);

        var result = await controller.Create(new RoomDto { Name = "New" }, CancellationToken.None);

        var created = Assert.IsType<RoomDto>(Assert.IsType<CreatedAtActionResult>(result).Value);
        Assert.Equal(4, created.SortOrder);
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
