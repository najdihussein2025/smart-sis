using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Tests.Features.Support;

/// <summary>SupportRequest entity behaviour: status changes stamp / clear ResolvedAt, closed requests take no replies.</summary>
public sealed class SupportRequestTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(SupportStatus.Resolved)]
    [InlineData(SupportStatus.Closed)]
    public void ChangeStatus_ToResolvedOrClosed_SetsResolvedAt(SupportStatus status)
    {
        var r = new SupportRequest();
        Assert.True(r.ChangeStatus(status, Now));
        Assert.Equal(status, r.Status);
        Assert.Equal(Now, r.ResolvedAt);
    }

    [Fact]
    public void ChangeStatus_ResolvedToClosed_KeepsFirstResolvedAt()
    {
        var r = new SupportRequest();
        r.ChangeStatus(SupportStatus.Resolved, Now);
        r.ChangeStatus(SupportStatus.Closed, Now.AddDays(2));
        Assert.Equal(Now, r.ResolvedAt);
    }

    [Theory]
    [InlineData(SupportStatus.Open)]
    [InlineData(SupportStatus.InProgress)]
    public void ChangeStatus_Reopened_ClearsResolvedAt(SupportStatus status)
    {
        var r = new SupportRequest { Status = SupportStatus.Resolved, ResolvedAt = Now };
        r.ChangeStatus(status, Now.AddDays(1));
        Assert.Null(r.ResolvedAt);
    }

    [Fact]
    public void ChangeStatus_SameStatus_ReturnsFalse()
    {
        var r = new SupportRequest { Status = SupportStatus.InProgress };
        Assert.False(r.ChangeStatus(SupportStatus.InProgress, Now));
        Assert.Null(r.ResolvedAt);
    }

    [Theory]
    [InlineData(SupportStatus.Open)]
    [InlineData(SupportStatus.InProgress)]
    [InlineData(SupportStatus.Resolved)]
    public void AddReply_NotClosed_AddsMessage(SupportStatus status)
    {
        var r = new SupportRequest { Id = 5, Status = status };
        var m = r.AddReply(7, "Hello");
        Assert.Same(m, Assert.Single(r.Messages));
        Assert.Equal(7, m.AuthorId);
        Assert.Equal("Hello", m.Body);
    }

    [Fact]
    public void AddReply_Closed_Throws()
    {
        var r = new SupportRequest { Status = SupportStatus.Closed };
        var ex = Assert.Throws<BusinessRuleException>(() => r.AddReply(7, "Hello"));
        Assert.Equal("request_closed", ex.Code);
        Assert.Empty(r.Messages);
    }
}
