using Xunit;
using System.Linq;

namespace WPCalculator.Tests
{
    public class WeakestPreconditionCalculatorTests
    {
       
        [Fact]
        public void TestDivisionDefinedness()
        {
            // Arrange
            var calculator = new WeakestPreconditionCalculator();
            string code = "result := a / b;";
            string postCondition = "result > 0";

            // Act
            var result = calculator.CalculateWP(code, postCondition);

            // Assert
            Assert.Contains("знаменатель != 0", result.FinalPrecondition);
            Assert.Contains("Условия определенности: знаменатель != 0", result.Steps);
        }

        [Fact]
        public void TestSqrtDefinedness()
        {
            // Arrange
            var calculator = new WeakestPreconditionCalculator();
            string code = "root := sqrt(x);";
            string postCondition = "root >= 0";

            // Act
            var result = calculator.CalculateWP(code, postCondition);

            // Assert
            Assert.Contains("выражение_под_корнем >= 0", result.FinalPrecondition);
            Assert.Contains("Условия определенности: выражение_под_корнем >= 0", result.Steps);
        }

        [Fact]
        public void TestComplexExpression()
        {
            // Arrange
            var calculator = new WeakestPreconditionCalculator();
            string code = "y := (a + b) / c * sqrt(d);";
            string postCondition = "y > 10";

            // Act
            var result = calculator.CalculateWP(code, postCondition);

            // Assert
            Assert.Contains("знаменатель != 0", result.FinalPrecondition);
            Assert.Contains("выражение_под_корнем >= 0", result.FinalPrecondition);
        }

        [Fact]
        public void TestMultipleVariables()
        {
            // Arrange
            var calculator = new WeakestPreconditionCalculator();
            string code = @"a := x + y;
b := a * 2;";
            string postCondition = "b > a";

            // Act
            var result = calculator.CalculateWP(code, postCondition);

            // Assert
            Assert.Contains("(x + y) * 2", result.FinalPrecondition);
            Assert.Contains("(x + y)", result.FinalPrecondition);
        }

        
        

        

        [Fact]
        public void TestInvalidIfStatementThrowsException()
        {
            // Arrange
            var calculator = new WeakestPreconditionCalculator();
            string code = "if x1 >= x2 max := x1;"; // Некорректный синтаксис
            string postCondition = "max > 100";

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                calculator.CalculateWP(code, postCondition));
        }
        [Fact]
        public void TestAbsFunction()
        {
            // Arrange
            var calculator = new WeakestPreconditionCalculator();
            string code = "result := abs(x);";
            string postCondition = "result >= 0";

            // Act
            var result = calculator.CalculateWP(code, postCondition);

            // Assert
            Assert.Contains("abs(x)", result.FinalPrecondition);
            Assert.DoesNotContain("знаменатель", result.FinalPrecondition);
            Assert.DoesNotContain("выражение_под_корнем", result.FinalPrecondition);
        }

    }
}