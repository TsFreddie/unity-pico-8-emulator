using NUnit.Framework;
using TsFreddie.Pico8;
using System;

public class MathAPITests
{
    ScriptProcessor executor = new ScriptProcessor();

    [Test]
    public void abs()
    {
        Assert.AreEqual(executor.Run(@"
           return abs(-2.5)
        ").Number, 2.5d);
    }

    [Test]
    public void atan2()
    {
        Assert.AreEqual(executor.Run(@"
           return atan2(1, 1)
        ").Number, 0.875d);
    }

    [Test]
    public void band()
    {
        Assert.AreEqual(executor.Run(@"
           return band(3, 2)
        ").Number, 2d);
    }

    [Test]
    public void bandf()
    {
        Assert.AreEqual(executor.Run(@"
           return band(3.25, 2.75)
        ").Number, 2.25d);
    }


    [Test]
    public void bnot()
    {
        Assert.AreEqual(executor.Run(@"
           return bnot(3.25)
        ").Number, -3.25d);
    }

    [Test]
    public void bandbnot()
    {
        Assert.AreEqual(executor.Run(@"
           return band(bnot(0xb), 0xf)
        ").Number, 4d);
    }

    [Test]
    public void bor()
    {
        Assert.AreEqual(executor.Run(@"
           return bor(0x5, 0x9)
        ").Number, 13d);
    }

    [Test]
    public void bxor()
    {
        Assert.AreEqual(executor.Run(@"
           return bxor(0x5, 0x9)
        ").Number, 12d);
    }

    [Test]
    public void cos()
    {
        double resultTrunc = (int)(executor.Run(@"
           return cos(0.875)
        ").Number * 10000) / 10000d;
        Assert.AreEqual(resultTrunc, 0.7071d);
    }

    [Test]
    public void flr()
    {
        Assert.AreEqual(executor.Run(@"
           return flr(-5.2)
        ").Number, -6d);
    }

    [Test]
    public void max()
    {
        Assert.AreEqual(executor.Run(@"
           return max(5,2)
        ").Number, 5d);
    }

    [Test]
    public void mid()
    {
        Assert.AreEqual(executor.Run(@"
           return mid(1,2,3)
        ").Number, 2d);
    }

    [Test]
    public void min()
    {
        Assert.AreEqual(executor.Run(@"
           return min(2,3)
        ").Number, 2d);
    }

    [Test]
    public void shl()
    {
        Assert.AreEqual(executor.Run(@"
           return shl(1, 15)
        ").Number, -32768d);
    }

    [Test]
    public void shr()
    {
        Assert.AreEqual(executor.Run(@"
           return shr(-32767, 16)
        ").Number, -0.5);
    }


    [Test]
    public void sin()
    {
        double resultTrunc = (int)(executor.Run(@"
           return sin(0.375)
        ").Number * 10000) / 10000d;
        Assert.AreEqual(resultTrunc, -0.7071d);
    }

    [Test]
    public void sqrt()
    {
        double resultTrunc = (int)(executor.Run(@"
           return sqrt(2)
        ").Number * 10000) / 10000d;
        Assert.AreEqual(resultTrunc, 1.4167d);
    }


    [Test]
    public void negsqrt()
    {
        double resultTrunc = (int)(executor.Run(@"
           return sqrt(-2)
        ").Number * 10000) / 10000d;
        Assert.AreEqual(resultTrunc, 1.1429d);
    }

    [Test]
    public void srand()
    {
        double resultTrunc = (int)(executor.Run(@"
            srand(1)
            rnd(20)
            rnd(20)
            return rnd(20)
        ").Number * 10000) / 10000d;
        Assert.AreEqual(resultTrunc, 16.5933d);
    }
}


public class TableAPITests
{
    ScriptProcessor executor = new ScriptProcessor();

    [Test]
    public void add()
    {
        Assert.IsTrue(executor.Run(@"
            t = {1, 3, 5}
            add(t, 7)
            add(t, 9)
            if (#t != 5) return false
            if (t[5] != 9) return false
            return true
        ").Boolean);
    }

    [Test]
    public void all()
    {
        Assert.Equals(executor.Run(@"
            t = {1, 3, 5}
            add(t, 7)
            add(t, 9)

            result = 0
            for v in all(t) do
              result += v
            end
        ").Number, 25d);
    }

    [Test]
    public void del()
    {
        Assert.IsTrue(executor.Run(@"
            t = {1, 3, 5, 3, 7, 5}
            del(t, 3)  -- t = {1, 5, 3, 7, 5}
            del(t, 7)  -- t = {1, 5, 3, 5}
            del(t, 5)  -- t = {1, 3, 5}
 
            del(t, 9)
            
            if (#t != 3) return false
            if (t[3] != 5) return false
            return True
        ").Boolean);
    }

    [Test]
    public void each()
    {
        Assert.IsTrue(executor.Run(@"
            isSeven = false
            function hasSeven(item)
                if (item == 7) isSeven = True
            end
            t = {1, 3, 'hello', 5, 7}
            foreach(t, hasSeven)
            return isSeven
        ").Boolean);
    }

    [Test]
    public void pairs()
    {
        Assert.IsTrue(executor.Run(@"
            keySum = 0
            valSum = 0
            t = {111, 222, 333}
            for k, v in pairs(t) do
                keySum += k
                valSum += v
            end
            if (keySum != 6) return false
            if (valSum != 666) return false
            return true
        ").Boolean);
    }
}

public class ObjectAPITests
{
    ScriptProcessor executor = new ScriptProcessor();

    [Test]
    public void tostr()
    {
        Assert.AreEqual(executor.Run(@"
            return tostr(4.855823)
        ").String, "4.8558");
    }
}
