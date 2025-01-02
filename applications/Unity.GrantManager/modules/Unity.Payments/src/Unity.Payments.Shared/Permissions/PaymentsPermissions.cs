using Volo.Abp.Reflection;

namespace Unity.Payments.Permissions;

public static class PaymentsPermissions
{
    public const string GroupName = "PaymentsPermissions";

    public static class Payments
    {
        public const string Default = GroupName + ".Payments";
        public const string L1ApproveOrDecline = Default + ".L1ApproveOrDecline";
        public const string L2ApproveOrDecline = Default + ".L2ApproveOrDecline";
        public const string L3ApproveOrDecline = Default + ".L3ApproveOrDecline";
        public const string Decline = Default + ".Decline";
        public const string RequestPayment = Default + ".RequestPayment";
        public const string EditSupplierInfo = Default + ".EditSupplierInfo";
    }    

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(PaymentsPermissions));
    }
}
