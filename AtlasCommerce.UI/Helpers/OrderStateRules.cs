namespace AtlasCommerce.UI.Helpers
{
    public static class OrderStateRules
    {
        public static bool CanChange(enmOrderStatus from, enmOrderStatus to)
        {
            return (from, to) switch
            {
                // Checkout => Payment
                (enmOrderStatus.Draft, enmOrderStatus.PendingPayment) => true,
                (enmOrderStatus.PendingPayment, enmOrderStatus.Paid) => true,

                // Payment => Fulfillment
                (enmOrderStatus.Paid, enmOrderStatus.Preparing) => true,
                (enmOrderStatus.Preparing, enmOrderStatus.Shipped) => true,
                (enmOrderStatus.Shipped, enmOrderStatus.Delivered) => true,

                // Cancellation rules
                (enmOrderStatus.PendingPayment, enmOrderStatus.Cancelled) => true,
                (enmOrderStatus.Paid, enmOrderStatus.Cancelled) => true,

                _ => false
            };
        }
    }
}
