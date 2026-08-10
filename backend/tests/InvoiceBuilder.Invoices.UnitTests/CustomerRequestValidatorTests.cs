using FluentValidation.TestHelper;
using InvoiceBuilder.Invoices.Contracts;
using Xunit;

namespace InvoiceBuilder.Invoices.UnitTests;

public class CustomerRequestValidatorTests
{
    private readonly CustomerRequestValidator _validator = new();

    private static CustomerRequest ValidRequest() => new(
        "Acme Corp", "John Doe", "123 Main St", "Springfield", "12345", "USA", "john@acme.test", "VAT-123");

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
    public void NameTooLong_HasValidationError()
    {
        var result = _validator.TestValidate(ValidRequest() with { Name = new string('a', 201) });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void MissingAddressLine_HasValidationError()
    {
        var result = _validator.TestValidate(ValidRequest() with { AddressLine = "" });
        result.ShouldHaveValidationErrorFor(x => x.AddressLine);
    }

    [Fact]
    public void MissingCity_HasValidationError()
    {
        var result = _validator.TestValidate(ValidRequest() with { City = "" });
        result.ShouldHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void MissingCountry_HasValidationError()
    {
        var result = _validator.TestValidate(ValidRequest() with { Country = "" });
        result.ShouldHaveValidationErrorFor(x => x.Country);
    }

    [Fact]
    public void InvalidEmail_HasValidationError()
    {
        var result = _validator.TestValidate(ValidRequest() with { Email = "not-an-email" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void NullEmail_HasNoValidationError()
    {
        // Email is optional; the EmailAddress format rule only kicks in via .When(non-empty).
        var result = _validator.TestValidate(ValidRequest() with { Email = null });
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void NullOptionalFields_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(ValidRequest() with { ContactName = null, PostalCode = null, TaxId = null });
        result.ShouldNotHaveAnyValidationErrors();
    }
}
