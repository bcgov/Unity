﻿using Volo.Abp.Reflection;

namespace Unity.Payments.Permissions;

public static class PaymentsPermissions
{
    public const string GroupName = "Payments";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(PaymentsPermissions));
    }
}