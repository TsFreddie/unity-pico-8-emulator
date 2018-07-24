using NUnit.Framework;
using TsFreddie.Pico8;

public class LuaSyntaxTests
{
    PicoEmulator executor = new PicoEmulator();

    [Test]
    public void IfShorthand()
    {
        Assert.AreEqual(executor.Run(@"
            i = 1 if (false) i = 2 else i = 5
            i = 1 if (true) i = 2 else i = 5
            if (i == 2) i = 3 else i = 5
            return i
        ").Number, 3.0d);
    }

    [Test]
    public void UnaryAddition()
    {
        Assert.AreEqual(executor.Run(@"
            i = 1
            i += 1 + 1i=5
            i+=1+1 i+=1+1
            return i
        ").Number, 9.0d);
    }

    [Test]
    public void UnarySubstract()
    {
        Assert.AreEqual(executor.Run(@"
            i = 4
            i -= 1 + 2
            return i
        ").Number, 1.0d);
    }

    [Test]
    public void UnaryMultiplication()
    {
        Assert.AreEqual(executor.Run(@"
            i = 1
            i *= 1 + 1
            return i
        ").Number, 2.0d);
    }

    [Test]
    public void UnaryDivision()
    {
        Assert.AreEqual(executor.Run(@"
            i = 2
            i /= 1 + 1
            return i
        ").Number, 1.0d);
    }

    [Test]
    public void UnaryModulo()
    {
        Assert.AreEqual(executor.Run(@"
            i = 3
            i %= 1 + 1
            return i
        ").Number, 1.0d);
    }

    [Test]
    public void AlternativeNotEqual()
    {
        Assert.AreEqual(executor.Run(@"
            return 1 != 1 and 2 != 2
        ").Boolean, false);
    }
}
