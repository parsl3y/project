using FluentAssertions;
using VetClinic.Api.Entities;
using VetClinic.Api.Exceptions;
using VetClinic.Api.Services;
using Xunit;
using System;

namespace VetClinic.Tests.Unit;

public class AppointmentValidationTests
{
    [Fact]
    public void ValidateFutureDate_FutureDate_DoesNotThrow()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        FluentActions.Invoking(() => AppointmentService.ValidateFutureDate(date))
            .Should().NotThrow();
    }

    [Fact]
    public void ValidateFutureDate_Today_ThrowsBusinessException()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        FluentActions.Invoking(() => AppointmentService.ValidateFutureDate(today))
            .Should().Throw<BusinessException>()
            .WithMessage("*майбутньому*");
    }

    [Fact]
    public void ValidateFutureDate_PastDate_ThrowsBusinessException()
    {
        var past = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        FluentActions.Invoking(() => AppointmentService.ValidateFutureDate(past))
            .Should().Throw<BusinessException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public void ValidateFutureDate_VariousFutureDays_DoesNotThrow(int days)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(days));
        FluentActions.Invoking(() => AppointmentService.ValidateFutureDate(date))
            .Should().NotThrow();
    }

    [Fact]
    public void ValidateNotCancelled_ScheduledStatus_DoesNotThrow()
    {
        var appt = new Appointment { Status = AppointmentStatus.Scheduled };
        FluentActions.Invoking(() => AppointmentService.ValidateNotCancelled(appt))
            .Should().NotThrow();
    }

    [Fact]
    public void ValidateNotCancelled_CancelledStatus_ThrowsBusinessException()
    {
        var appt = new Appointment { Status = AppointmentStatus.Cancelled };
        FluentActions.Invoking(() => AppointmentService.ValidateNotCancelled(appt))
            .Should().Throw<BusinessException>()
            .WithMessage("*Скасований*");
    }

    [Fact]
    public void ValidateNotCancelled_CompletedStatus_DoesNotThrow()
    {
        var appt = new Appointment { Status = AppointmentStatus.Completed };
        FluentActions.Invoking(() => AppointmentService.ValidateNotCancelled(appt))
            .Should().NotThrow();
    }
}