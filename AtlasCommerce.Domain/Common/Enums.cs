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

    [Description("Teslim Edildi")]
    Delivered = 5,

    [Description("İptal Edildi")]
    Cancelled = 6,

    [Description("İade Talebi")]
    ReturnRequested = 7,

    [Description("İade Edildi")]
    Returned = 8
}