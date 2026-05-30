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
    [Description("Taslak / Kaydedildi")]
    Draft = 0,

    [Description("Ödeme Bekleniyor")]
    PendingPayment = 1,

    [Description("Ödendi")]
    Paid = 2,

    [Description("Hazırlanıyor")]
    Preparing = 3,

    [Description("Kargoya Verildi")]
    Shipped = 4,

    [Description("Mağazada Alım Bekliyor")] // Mağazada Teslim için.
    WaitingInStore = 5,

    [Description("Teslim Edildi")]
    Delivered = 6,

    [Description("İptal Edildi")]
    Cancelled = 7,

    [Description("İade Talebi")]
    ReturnRequested = 8,

    [Description("İade Edildi")]
    Returned = 9
}

public enum enmPaymentStatus
{
    [Description("Ödeme Bekleniyor")]
    Pending = 0,

    [Description("Ödeme Onaylandı")]
    Authorized = 1,

    [Description("Ödeme Tamamlandı")]
    Paid = 2,

    [Description("Ödeme Başarısız")]
    Failed = 3,

    [Description("İptal Edildi")]
    Cancelled = 4,

    [Description("İade Edildi")]
    Refunded = 5,

    [Description("Kısmi İade")]
    PartiallyRefunded = 6
}

public enum enmReturnStatus
{
    [Description("İade Yok")]
    None = 0,

    [Description("İade Talep Edildi")]
    Requested = 1,

    [Description("İade Onaylandı")]
    Approved = 2,

    [Description("İade Reddedildi")]
    Rejected = 3,

    [Description("Ürün Teslim Alındı")]
    Received = 4,

    [Description("Para İadesi Yapıldı")]
    Refunded = 5
}