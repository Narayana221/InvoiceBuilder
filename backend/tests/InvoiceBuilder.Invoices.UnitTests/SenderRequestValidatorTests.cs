using FluentValidation.TestHelper;
using InvoiceBuilder.Invoices.Contracts;
using Xunit;

namespace InvoiceBuilder.Invoices.UnitTests;

public class SenderRequestValidatorTests
{
    private readonly SenderRequestValidator _validator = new();

    private static SenderRequest ValidRequest() => new(
        "My Company LLC", "Alice Smith", "456 Market St", "City", null, "USA", null, "TAX-987654",
        "IBAN XX00 0000 0000 0000 00");

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MissingName_HasValidationError()
    {
        var result = _validator.TestValidate(ValidRequest() with { Name = "" });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void MissingAddressLine_HasValidationError()
    {
        var result = _validator.TestValidate(ValidRequest() with { AddressLine = "" });
        result.ShouldHaveValidationErrorFor(x => x.AddressLine);
    }

    [Fact]
    public void InvalidEmail_HasValidationError()
    {
        var result = _validator.TestValidate(ValidRequest() with { Email = "not-an-email" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void BankDetailsTooLong_HasValidationError()
    {
        var result = _validator.TestValidate(ValidRequest() with { BankDetails = new string('a', 201) });
        result.ShouldHaveValidationErrorFor(x => x.BankDetails);
    }

    [Fact]
    public void NullOptionalFields_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(ValidRequest() with
        {
            ContactName = null,
            PostalCode = null,
            Email = null,
            TaxId = null,
            BankDetails = null,
        });
        result.ShouldNotHaveAnyValidationErrors();
    }
}
