using ExhaustiveSwitchOnEnums.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExhaustiveSwitchOnEnums.Tests
{
    [TestClass]
    public class ExhaustiveSwitchOnEnumsTests : CodeFixVerifier
    {
        #region Case 1

        private const string SwitchNotExhaustive1 = @"
using System;

namespace ExhaustiveSwitchOnEnums
{
    public enum Status
    {
        Accepted = 1,
        Cooking = 2,
        Cooked = 3,
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
        }

        public static void Method(Status status)
        {
            switch (status)
            {
                case Status.Accepted:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}";

        private const string SwitchNotExhaustiveFixed1 = @"
using System;

namespace ExhaustiveSwitchOnEnums
{
    public enum Status
    {
        Accepted = 1,
        Cooking = 2,
        Cooked = 3,
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
        }

        public static void Method(Status status)
        {
            switch (status)
            {
                case Status.Accepted:
                    return;
                case Status.Cooking:
                    throw new NotImplementedException();
                case Status.Cooked:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}";

        #endregion

        #region Case 2

        private const string SwitchNotExhaustive2 = @"
using System;

namespace ExhaustiveSwitchOnEnums
{
    public enum Status
    {
        Accepted = 1,
        Cooking = 2,
        Cooked = 3,
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
        }

        public static void Method(Status status)
        {
            switch (status)
            {
                case Status.Accepted:
                    return;
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
                }
            }
        }
    }
}";

        private const string SwitchNotExhaustiveFixed2 = @"
using System;

namespace ExhaustiveSwitchOnEnums
{
    public enum Status
    {
        Accepted = 1,
        Cooking = 2,
        Cooked = 3,
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
        }

        public static void Method(Status status)
        {
            switch (status)
            {
                case Status.Accepted:
                    return;
                case Status.Cooking:
                    throw new NotImplementedException();
                case Status.Cooked:
                    throw new NotImplementedException();
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
                }
            }
        }
    }
}";

        #endregion

        #region Case 3

        private const string SwitchNotExhaustive3 = @"
using System;

namespace ExhaustiveSwitchOnEnums
{
    public enum Status
    {
        Accepted = 1,
        Cooking = 2,
        Cooked = 3,
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
        }

        public static string Method(Status status)
        {
            return status switch
            {
                Status.Accepted => """",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
    }
}";

        private const string SwitchNotExhaustiveFixed3 = @"
using System;

namespace ExhaustiveSwitchOnEnums
{
    public enum Status
    {
        Accepted = 1,
        Cooking = 2,
        Cooked = 3,
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
        }

        public static string Method(Status status)
        {
            return status switch
            {
                Status.Accepted => """",
                Status.Cooking => throw new NotImplementedException(),
                Status.Cooked => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
    }
}";

        #endregion

        [DataTestMethod]
        [DataRow(SwitchNotExhaustiveFixed1)]
        [DataRow(SwitchNotExhaustiveFixed2)]
        [DataRow(SwitchNotExhaustiveFixed3)]
        public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
        {
            VerifyCSharpDiagnostic(testCode);
        }

        [DataTestMethod]
        [DataRow(SwitchNotExhaustive1, SwitchNotExhaustiveFixed1, 21, 13)]
        [DataRow(SwitchNotExhaustive2, SwitchNotExhaustiveFixed2, 21, 13)]
        [DataRow(SwitchNotExhaustive3, SwitchNotExhaustiveFixed3, 21, 20)]
        public void WhenDiagnosticIsRaisedFixUpdatesCode(string test, string fixTest, int line, int column)
        {
            var expected = new DiagnosticResult
            {
                Id = ExhaustiveSwitchOnEnumsAnalyzer.DiagnosticId,
                Message = new LocalizableResourceString(
                    nameof(Resources.AnalyzerMessageFormat),
                    Resources.ResourceManager,
                    typeof(Resources)).ToString(),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line, column)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
            VerifyCSharpFix(test, fixTest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ExhaustiveSwitchOnEnumsCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ExhaustiveSwitchOnEnumsAnalyzer();
        }
    }
}