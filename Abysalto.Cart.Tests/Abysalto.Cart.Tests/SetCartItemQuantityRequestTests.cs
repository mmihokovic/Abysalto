using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Abysalto.Cart.Api.Contracts;
using Xunit;

namespace Abysalto.Cart.Tests;

public sealed class SetCartItemQuantityRequestTests
{
    [Fact]
    public void Quantity_RejectsZero()
    {
        var request = new SetCartItemQuantityRequest { Quantity = 0 };
        var errors = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request, new ValidationContext(request), errors, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.Quantity)));
    }
}
