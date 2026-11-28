using FluentAssertions;
using NyangoPress.Application.Customers;
using NyangoPress.Domain.Customers;
using NyangoPress.Shared.Customers;
using SharedCustomerType = NyangoPress.Shared.Customers.CustomerType;

namespace NyangoPress.UnitTests.Customers;

public class CustomerValidatorTests
{
    [Fact]
    public void NormalizationHelpers_NormalizePhoneAndNameConsistently()
    {
        Customer.NormalizePhoneDigits("+237 690-12 34 56").Should().Be("237690123456");
        Customer.NormalizeText("  Amina   MBARGA  ").Should().Be("amina mbarga");
    }

    [Fact]
    public void CreateCustomerValidator_RejectsInvalidInputs()
    {
        var validator = new CreateCustomerValidator();

        validator.Validate(new CreateCustomerRequest(SharedCustomerType.Individual, "", "123", null, "bad-email", null, null))
            .IsValid.Should().BeFalse();

        validator.Validate(new CreateCustomerRequest(SharedCustomerType.Individual, "Amina Mbarga", "+237690123456", null, "amina@exemple.cm", null, null))
            .IsValid.Should().BeTrue();
    }
}
