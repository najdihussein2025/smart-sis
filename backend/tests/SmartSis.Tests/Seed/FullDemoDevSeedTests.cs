using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSis.Api.Features.Cart.Models.Entities;
using SmartSis.Api.Features.Career.Models.Entities;
using SmartSis.Api.Features.Chat.Models.Entities;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Seed;

/// <summary>The opt-in FullDemoDevSeed (Seed:FullDemo) runs cleanly and fills every demo account.</summary>
public sealed class FullDemoDevSeedTests
{
    [Fact]
    public async Task FullDemo_On_FillsAllThreeAccounts()
    {
        using var baseFactory = new SisApiFactory();
        using var factory = baseFactory.WithWebHostBuilder(b => b.UseSetting("Seed:FullDemo", "true"));
        using var _ = factory.CreateClient(); // starts the host, which seeds

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SisDbContext>();
        var ids = await db.Set<SmartSis.Api.Features.Users.Models.Entities.User>()
            .ToDictionaryAsync(u => u.UniversityId, u => u.Id);

        Assert.True(await db.Set<CartItem>().CountAsync(c => c.StudentId == ids[3001]) >= 2);
        Assert.True(await db.Set<EnrollmentAction>().CountAsync(a => a.StudentId == ids[3001]) >= 4);
        Assert.NotNull(await db.Set<CareerAssessment>().Include(a => a.TraitScores).SingleOrDefaultAsync(a => a.StudentId == ids[3001]));
        Assert.True(await db.Set<ChatConversation>().AnyAsync(c => c.StudentId == ids[3001]));
        foreach (var id in new[] { 1001L, 2001L, 3001L })
            Assert.True(await db.Set<Notification>().AnyAsync(n => n.UserId == ids[id] && n.ReadAt == null), $"no unread notification for {id}");
        Assert.True(await db.Set<TermResult>().CountAsync(r => r.StudentId == ids[3001]) >= 2);
    }

    [Fact]
    public async Task FullDemo_Off_LeavesTheBaseSeedAlone()
    {
        using var factory = new SisApiFactory();
        using var _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SisDbContext>();
        Assert.False(await db.Set<CareerAssessment>().AnyAsync());
        Assert.False(await db.Set<CartItem>().AnyAsync());
    }
}
