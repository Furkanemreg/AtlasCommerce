namespace AtlasCommerce.UI.Helpers
{
    public static class PaymentStateRules
    {
        public static bool CanChange(enmPaymentStatus from, enmPaymentStatus to)
        {
            return (from, to) switch
            {
                (enmPaymentStatus.Pending, enmPaymentStatus.Authorized) => true,
                (enmPaymentStatus.Authorized, enmPaymentStatus.Paid) => true,

                (enmPaymentStatus.Pending, enmPaymentStatus.Failed) => true,
                (enmPaymentStatus.Authorized, enmPaymentStatus.Failed) => true,

                (enmPaymentStatus.Paid, enmPaymentStatus.Refunded) => true,
                (enmPaymentStatus.Paid, enmPaymentStatus.PartiallyRefunded) => true,

                _ => false
            };
        }
    }
}
