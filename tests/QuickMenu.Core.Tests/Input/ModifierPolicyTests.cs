using QuickMenu.Core.Input;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class ModifierPolicyTests
{
    [Theory]
    [InlineData(true, PinModifier.Alt)]
    [InlineData(false, PinModifier.Ctrl)]
    public void Not_user_set_uses_mint_default(bool mint, PinModifier expected)
    {
        Assert.Equal(expected, ModifierPolicy.Effective(false, PinModifier.Shift, mint));
    }

    [Theory]
    [InlineData(PinModifier.Ctrl, true)]
    [InlineData(PinModifier.Alt, false)]
    [InlineData(PinModifier.Shift, true)]
    [InlineData(PinModifier.Shift, false)]
    public void User_set_wins(PinModifier choice, bool mint)
    {
        Assert.Equal(choice, ModifierPolicy.Effective(true, choice, mint));
    }
}
