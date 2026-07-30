using System;
using Shouldly;
using Unity.Reporting.EntityFrameworkCore.Repositories;
using Xunit;

namespace Unity.Reporting.Application.Tests.EntityFrameworkCore.Repositories
{
    /// <summary>
    /// Regression tests for AB#33409 - SQL Injection via the ViewDataRequest Filter/OrderBy
    /// parameters accepted by ReportColumnsMapRepository.GetViewDataAsync/GetViewPreviewDataAsync.
    /// </summary>
    public class ReportColumnsMapRepositorySqlSafetyTests
    {
        private static readonly string[] ValidColumns = ["id", "status", "amount", "created_date", "applicant_name"];

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Stacked_Statement_Injection()
        {
            const string maliciousFilter = "1=1; DROP TABLE \"Applications\";--";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, ValidColumns));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Union_Based_Injection()
        {
            const string maliciousFilter = "id = 1 UNION SELECT rolpassword, 1, 1 FROM pg_authid --";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, ValidColumns));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Function_Call_Injection()
        {
            const string maliciousFilter = "id = 1 OR pg_sleep(10) IS NOT NULL";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, ValidColumns));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Column_Not_In_Allow_List()
        {
            const string filter = "secret_column = 'x'";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Function_Call_Via_Column_Named_Like_Dangerous_Function()
        {
            // A column can legitimately be named "pg_sleep" (SanitizeColumnName has no
            // function-name blocklist), so the allow-list check alone isn't enough - the
            // validator must also reject "identifier(" as function-call syntax.
            string[] columnsIncludingDangerousName = ["id", "status", "pg_sleep"];
            const string maliciousFilter = "pg_sleep(10) IS NOT NULL";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, columnsIncludingDangerousName));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_In_Clause_Without_Treating_It_As_A_Function_Call()
        {
            const string filter = "status IN ('Active', 'Pending')";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_Legitimate_Filter()
        {
            const string filter = "status = 'Active' AND amount > 1000";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Return_Empty_For_Blank_Input()
        {
            ReportColumnsMapRepository.ValidateFilterExpression(null, ValidColumns).ShouldBe(string.Empty);
            ReportColumnsMapRepository.ValidateFilterExpression("   ", ValidColumns).ShouldBe(string.Empty);
        }

        [Fact]
        public void ValidateOrderByExpression_Should_Reject_Stacked_Statement_Injection()
        {
            const string maliciousOrderBy = "id; DROP TABLE \"Applications\";--";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateOrderByExpression(maliciousOrderBy, ValidColumns));
        }

        [Fact]
        public void ValidateOrderByExpression_Should_Reject_Column_Not_In_Allow_List()
        {
            const string orderBy = "secret_column DESC";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateOrderByExpression(orderBy, ValidColumns));
        }

        [Fact]
        public void ValidateOrderByExpression_Should_Accept_Legitimate_OrderBy()
        {
            const string orderBy = "\"created_date\" DESC, id ASC";

            var result = ReportColumnsMapRepository.ValidateOrderByExpression(orderBy, ValidColumns);

            result.ShouldBe(orderBy);
        }

        [Fact]
        public void ValidateOrderByExpression_Should_Return_Empty_For_Blank_Input()
        {
            ReportColumnsMapRepository.ValidateOrderByExpression(null, ValidColumns).ShouldBe(string.Empty);
            ReportColumnsMapRepository.ValidateOrderByExpression("   ", ValidColumns).ShouldBe(string.Empty);
        }

        [Theory]
        [InlineData("my_view")]
        [InlineData("_leading_underscore")]
        [InlineData("view123")]
        public void IsValidPostgreSqlIdentifier_Should_Accept_Well_Formed_Identifiers(string identifier)
        {
            ReportColumnsMapRepository.IsValidPostgreSqlIdentifier(identifier).ShouldBeTrue();
        }

        [Theory]
        [InlineData("weird\"\"view")]
        [InlineData("view; DROP TABLE \"Applications\";--")]
        [InlineData("view name with spaces")]
        [InlineData("123_starts_with_digit")]
        [InlineData("")]
        public void IsValidPostgreSqlIdentifier_Should_Reject_Malformed_Or_Malicious_Identifiers(string identifier)
        {
            ReportColumnsMapRepository.IsValidPostgreSqlIdentifier(identifier).ShouldBeFalse();
        }
    }
}
