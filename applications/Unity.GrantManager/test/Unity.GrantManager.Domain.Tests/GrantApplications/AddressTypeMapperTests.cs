using System;
using Shouldly;
using Unity.GrantManager.GrantApplications;
using Xunit;

namespace Unity.GrantManager.Domain.Tests.GrantApplications
{
    /// <summary>
    /// <see cref="AddressTypeMapper"/> is a pure static translation with no dependencies,
    /// so these tests deliberately do not inherit the ABP domain test base.
    /// </summary>
    public class AddressTypeMapperTests
    {
        [Theory]
        [InlineData("MAILING")]
        [InlineData("  Mailing  ")]
        public void FromPortalValue_IgnoresCasingAndSurroundingWhitespace(string portalValue)
        {
            AddressTypeMapper.FromPortalValue(portalValue).ShouldBe(AddressType.MailingAddress);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public void FromPortalValue_MissingValue_ReturnsDefault(string? portalValue)
        {
            AddressTypeMapper.FromPortalValue(portalValue).ShouldBe(AddressTypeMapper.DefaultAddressType);
        }

        /// <summary>
        /// A non-empty but unrecognised value gets past the missing-value guard and matches no
        /// member, so the mapper must still fall back to the default. Each case guards a
        /// different way that could break: "PhysicalAddress" is the full member name rather
        /// than the short name, "Mail" is a prefix of a valid name, and "0" would be accepted
        /// by an Enum.TryParse-based rewrite and yield an undefined member.
        /// </summary>
        [Theory]
        [InlineData("Residential")]
        [InlineData("PhysicalAddress")]
        [InlineData("Mail")]
        [InlineData("0")]
        public void FromPortalValue_UnrecognisedValue_ReturnsDefault(string portalValue)
        {
            AddressTypeMapper.FromPortalValue(portalValue).ShouldBe(AddressTypeMapper.DefaultAddressType);
        }

        /// <summary>
        /// Pins the exact short names exchanged with the applicant portal. The round-trip test
        /// below deliberately does not assert literals, so this is what holds the wire contract.
        /// </summary>
        [Theory]
        [InlineData(AddressType.PhysicalAddress, "Physical")]
        [InlineData(AddressType.MailingAddress, "Mailing")]
        [InlineData(AddressType.BusinessAddress, "Business")]
        public void ToDisplayName_StripsTheAddressSuffix(AddressType addressType, string expected)
        {
            AddressTypeMapper.ToDisplayName(addressType).ShouldBe(expected);
        }

        /// <summary>
        /// Guards the round trip for every member, so a new AddressType cannot be added without
        /// the portal short name resolving back to it.
        /// </summary>
        [Fact]
        public void FromPortalValue_RoundTripsEveryDefinedMember()
        {
            foreach (var addressType in Enum.GetValues<AddressType>())
            {
                var displayName = AddressTypeMapper.ToDisplayName(addressType);

                AddressTypeMapper.FromPortalValue(displayName).ShouldBe(addressType);
            }
        }
    }
}
