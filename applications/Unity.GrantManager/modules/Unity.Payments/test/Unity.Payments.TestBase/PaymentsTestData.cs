using System;
using Volo.Abp.Data;
using Volo.Abp.Users;

namespace Unity.Payments;

public static class PaymentsTestData
{
    public static readonly Guid Tenant1 = Guid.NewGuid();
    public static readonly Guid User1Id = new("a5caf004-5742-4fc4-a97b-41bc8fae9280");
    public static readonly Guid User2Id = new("df02db6b-7cd2-4dcc-be1b-af649bfe3f1b");
    public static readonly Guid User3Id = new("8c01f807-2981-4d1f-bb42-d9b84fec8f77");
    public static readonly Guid User4Id = new("101ea667-16d2-4d4e-adf9-e7bb80b6625c");
    public static readonly Guid User5Id = new("2788b9e3-85d9-40e8-bb9b-fb3b696811d5");

    public static class UserDataMocks
    {
        public static UserData User1 => new(
            id: User1Id,
            userName: "defaultUser1",
            email: "user1@gov.bc.ca.test",
            name: "John",
            surname: "DefaultCurrentUser",
            emailConfirmed: true,
            phoneNumber: "1234567890",
            phoneNumberConfirmed: true,
            tenantId: Tenant1,
            isActive: true
        );

        public static UserData User2 => new(
            id: User2Id,
            userName: "user2",
            email: "user2@gov.bc.ca.test",
            name: "Jane",
            surname: "Smith",
            emailConfirmed: false,
            phoneNumber: "0987654321",
            phoneNumberConfirmed: false,
            tenantId: Tenant1,
            isActive: true
        );

        public static UserData User3 => new(
            id: User3Id,
            userName: "user3",
            email: "user3@gov.bc.ca.test",
            name: "Alice",
            surname: "Johnson",
            tenantId: Tenant1,
            isActive: false
        );

        public static UserData User4 => new(
            id: User4Id,
            userName: "user4",
            email: "user4@gov.bc.ca.test",
            name: "Bob",
            surname: "Brown",
            emailConfirmed: true,
            phoneNumber: "2233445566",
            phoneNumberConfirmed: false,
            tenantId: Tenant1,
            isActive: true
        );

        public static UserData User5 => new(
            id: User5Id,
            userName: "user5",
            email: "user5@gov.bc.ca.test",
            name: "Charlie",
            surname: "Davis",
            emailConfirmed: false,
            phoneNumber: "3344556677",
            phoneNumberConfirmed: true,
            tenantId: Tenant1,
            isActive: false
        );
    }
}
