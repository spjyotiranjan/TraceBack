using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using TraceBack.Models;

namespace TraceBack.Tests;

public sealed class ReportDateTests
{
    [Theory]
    [InlineData("2026-11-05T20:00:00Z", 330, "2026-11-05", "2026-11-06")]
    [InlineData("2026-11-06T01:00:00Z", -480, "2026-11-06", "2026-11-05")]
    public void Default_and_validation_use_utc_when_local_calendar_differs(
        string instant, int offsetMinutes, string utcDate, string localDate)
    {
        var clock = new ReportClock(instant, offsetMinutes);
        Assert.Equal(localDate, clock.GetLocalNow().ToString("yyyy-MM-dd"));
        var form = new ItemForm(clock);
        Assert.Equal(utcDate, form.DateReported.ToString("yyyy-MM-dd"));
        Assert.Equal(DateTimeKind.Utc, form.DateReported.Kind);
        using var services = new ServiceCollection().AddSingleton<TimeProvider>(clock).BuildServiceProvider();
        var context = new ValidationContext(form, services, null);
        Assert.DoesNotContain(form.Validate(context), error => error.MemberNames.Contains(nameof(ItemForm.DateReported)));
        form.DateReported = form.DateReported.AddDays(1);
        Assert.Contains(form.Validate(context), error => error.MemberNames.Contains(nameof(ItemForm.DateReported)));
    }

    [Fact]
    public void Parameterless_form_defaults_to_current_utc_date()
    {
        var before = DateTime.UtcNow.Date;
        var form = new ItemForm();
        Assert.InRange(form.DateReported, before, DateTime.UtcNow.Date);
        Assert.Equal(DateTimeKind.Utc, form.DateReported.Kind);
    }

    internal sealed class ReportClock(string instant, int offsetMinutes) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture);
        public override TimeZoneInfo LocalTimeZone { get; } = TimeZoneInfo.CreateCustomTimeZone(
            "Report test zone", TimeSpan.FromMinutes(offsetMinutes), "Report test zone", "Report test zone");
    }
}
