using System.ComponentModel;

public enum enmPlatform
{
    [Description("Website")]
    Website = 0,

    [Description("Trendyol")]
    Trendyol = 1,

    [Description("Hepsiburada")]
    Hepsiburada = 2,

    [Description("Shopify")]
    Shopify = 3
}

public enum enmOrderStatus
{
    [Description("Draft")]
    Draft = 0,

    [Description("Pending Payment")]
    PendingPayment = 1,

    [Description("Paid")]
    Paid = 2,

    [Description("Preparing")]
    Preparing = 3,

    [Description("Shipped")]
    Shipped = 4,

    [Description("Waiting In Store")] // Mağazada Teslim için.
    WaitingInStore = 5,

    [Description("Delivered")]
    Delivered = 6,

    [Description("Cancelled")]
    Cancelled = 7,

    [Description("Return Requested")]
    ReturnRequested = 8,

    [Description("Returned")]
    Returned = 9
}

public enum enmPaymentStatus
{
    [Description("Pending")]
    Pending = 0,

    [Description("Authorized")]
    Authorized = 1,

    [Description("Paid")]
    Paid = 2,

    [Description("Failed")]
    Failed = 3,

    [Description("Cancelled")]
    Cancelled = 4,

    [Description("Refunded")]
    Refunded = 5,

    [Description("Partially Refunded")]
    PartiallyRefunded = 6
}

public enum enmReturnStatus
{
    [Description("None")]
    None = 0,

    [Description("Requested")]
    Requested = 1,

    [Description("Approved")]
    Approved = 2,

    [Description("Rejected")]
    Rejected = 3,

    [Description("Received")]
    Received = 4,

    [Description("Refunded")]
    Refunded = 5
}