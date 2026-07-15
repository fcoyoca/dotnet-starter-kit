using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Features.v1.Claims.MarkReady;
using FSH.Modules.Claims.Features.v1.Claims.Submit;
using FSH.Modules.Claims.Features.v1.Claims.MarkPaid;
using FSH.Modules.Claims.Features.v1.Claims.MarkDenied;
using FSH.Modules.Claims.Features.v1.Claims.Void;
using Shouldly;
using Xunit;

namespace Claims.Tests.Validators;

public sealed class ClaimValidatorsTests
{
    [Fact]
    public void MarkReady_Rejects_Empty_Id()
    {
        var result = new MarkClaimReadyCommandValidator().Validate(new MarkClaimReadyCommand(Guid.Empty));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void MarkReady_Accepts_Valid_Id()
    {
        var result = new MarkClaimReadyCommandValidator().Validate(new MarkClaimReadyCommand(Guid.NewGuid()));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Submit_Rejects_Empty_Id()
    {
        var result = new SubmitClaimCommandValidator().Validate(new SubmitClaimCommand(Guid.Empty));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Submit_Accepts_Valid_Id()
    {
        var result = new SubmitClaimCommandValidator().Validate(new SubmitClaimCommand(Guid.NewGuid()));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void MarkPaid_Rejects_Empty_Id()
    {
        var result = new MarkClaimPaidCommandValidator().Validate(new MarkClaimPaidCommand(Guid.Empty));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void MarkPaid_Accepts_Valid_Id()
    {
        var result = new MarkClaimPaidCommandValidator().Validate(new MarkClaimPaidCommand(Guid.NewGuid()));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void MarkDenied_Rejects_Empty_Id()
    {
        var result = new MarkClaimDeniedCommandValidator().Validate(new MarkClaimDeniedCommand(Guid.Empty));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void MarkDenied_Accepts_Valid_Id()
    {
        var result = new MarkClaimDeniedCommandValidator().Validate(new MarkClaimDeniedCommand(Guid.NewGuid()));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Void_Rejects_Empty_Id()
    {
        var result = new VoidClaimCommandValidator().Validate(new VoidClaimCommand(Guid.Empty, null));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Void_Rejects_Reason_Over_MaxLength()
    {
        var result = new VoidClaimCommandValidator().Validate(new VoidClaimCommand(Guid.NewGuid(), new string('x', 513)));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Void_Accepts_Valid_Id_And_Null_Reason()
    {
        var result = new VoidClaimCommandValidator().Validate(new VoidClaimCommand(Guid.NewGuid(), null));
        result.IsValid.ShouldBeTrue();
    }
}
