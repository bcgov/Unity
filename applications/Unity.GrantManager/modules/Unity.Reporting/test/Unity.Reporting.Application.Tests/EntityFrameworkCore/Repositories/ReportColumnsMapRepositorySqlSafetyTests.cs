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
        public void ValidateFilterExpression_Should_Reject_Block_Comment_Injection()
        {
            const string maliciousFilter = "status = 'Active' /* comment */ OR 1=1";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, ValidColumns));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Trailing_Comment_Injection()
        {
            const string maliciousFilter = "status = 'Active' --comment";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, ValidColumns));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Unterminated_String_Literal()
        {
            const string maliciousFilter = "applicant_name = 'unterminated";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, ValidColumns));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Backslash_Quote_Breakout_Attempt()
        {
            // Postgres defaults to standard_conforming_strings=on, so a backslash does not escape
            // the following quote. If our tokenizer treated it as an escape (MySQL-style), the
            // string literal would swallow the rest of the payload instead of ending at the first
            // unescaped quote, and "OR 1=1--" would slip through unnoticed.
            const string maliciousFilter = "applicant_name = 'test\\' OR 1=1--'";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, ValidColumns));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Function_Call_With_Whitespace_Before_Paren()
        {
            // A space between the identifier and "(" is a classic bypass for naive "identifier("
            // checks - our tokenizer must not reset the "previous token was a column" state on
            // whitespace.
            string[] columnsIncludingDangerousName = ["id", "status", "pg_sleep"];
            const string maliciousFilter = "pg_sleep (10) IS NOT NULL";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, columnsIncludingDangerousName));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Function_Call_Via_Quoted_Identifier()
        {
            string[] columnsIncludingDangerousName = ["id", "status", "pg_sleep"];
            const string maliciousFilter = "\"pg_sleep\"(10) IS NOT NULL";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, columnsIncludingDangerousName));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Reject_Union_Injection_Regardless_Of_Keyword_Casing()
        {
            const string maliciousFilter = "id = 1 uNioN Select 1";

            Should.Throw<ArgumentException>(() =>
                ReportColumnsMapRepository.ValidateFilterExpression(maliciousFilter, ValidColumns));
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_In_Clause_Without_Treating_It_As_A_Function_Call()
        {
            const string filter = "status IN ('Active', 'Pending')";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_In_Clause_With_Numbers_And_No_Spacing()
        {
            const string filter = "id IN (1,2,3)";

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
        public void ValidateFilterExpression_Should_Accept_String_Value_That_Contains_Comment_Like_Substring()
        {
            // The value itself is attacker-adjacent-looking text, but it is safely contained inside
            // a quoted string literal, so it must be treated as ordinary data, not SQL syntax.
            const string filter = "status = 'Active; not-a-comment --'";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_Escaped_Quote_In_String_Literal()
        {
            const string filter = "applicant_name = 'O''Brien'";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_Grouped_Expression_With_Nested_Parens()
        {
            const string filter = "(status = 'Active' OR status = 'Pending') AND amount > 100";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_Between_Operator()
        {
            const string filter = "amount BETWEEN 100 AND 1000";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_Is_Not_Null()
        {
            const string filter = "created_date IS NOT NULL";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_Like_Operator()
        {
            const string filter = "applicant_name LIKE 'Smith%'";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_Lowercase_Keywords()
        {
            const string filter = "status = 'Active' and amount > 100";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_Not_With_Grouped_Expression()
        {
            const string filter = "NOT (status = 'Active')";

            var result = ReportColumnsMapRepository.ValidateFilterExpression(filter, ValidColumns);

            result.ShouldBe(filter);
        }

        [Fact]
        public void ValidateFilterExpression_Should_Accept_Decimal_Number_Literal()
        {
            const string filter = "amount >= 100.50";

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
