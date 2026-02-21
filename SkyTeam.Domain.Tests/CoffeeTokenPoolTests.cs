using System;
using FluentAssertions;

namespace SkyTeam.Domain.Tests;

public class CoffeeTokenPoolTests
{
    [Fact]
    public void Earn_ShouldCapAtThree_WhenExceedingCap()
    {
        // Arrange
        var pool = new CoffeeTokenPool(count: 2);

        // Act
        var result = pool.Earn(amount: 2);

        // Assert
        result.Count.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Earn_ShouldThrow_WhenAmountIsNonPositive(int amount)
    {
        // Arrange
        var pool = new CoffeeTokenPool(count: 1);

        // Act
        var act = () => pool.Earn(amount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void ctor_ShouldThrow_WhenCountIsOutOfRange(int count)
    {
        // Arrange
        // Act
        var creating = () => new CoffeeTokenPool(count);

        // Assert
        creating.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Spend_ShouldThrow_WhenAmountIsNonPositive(int amount)
    {
        // Arrange
        var pool = new CoffeeTokenPool(count: 1);

        // Act
        var act = () => pool.Spend(amount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Spend_ShouldThrow_WhenInsufficientTokens()
    {
        // Arrange
        var pool = new CoffeeTokenPool(count: 1);

        // Act
        var act = () => pool.Spend(amount: 2);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Spend_ShouldDecreaseCount_WhenSufficientTokens()
    {
        // Arrange
        var pool = new CoffeeTokenPool(count: 3);

        // Act
        var result = pool.Spend(amount: 2);

        // Assert
        result.Count.Should().Be(1);
    }

    [Fact]
    public void Spend_ShouldDecreaseCount_WhenAmountIsOne()
    {
        // Arrange
        var pool = new CoffeeTokenPool(count: 2);

        // Act
        var result = pool.Spend(amount: 1);

        // Assert
        result.Count.Should().Be(1);
    }
}
