namespace PersonalFinance.Web.Data.Entities;

public static class PaymentTypeExtensions
{
    public static string ToDisplayName(this PaymentType paymentType) => paymentType switch
    {
        PaymentType.Cash => "Cash",
        PaymentType.CreditCard => "Credit card",
        PaymentType.VoucherCard => "Voucher card",
        PaymentType.Debit => "Debit",
        _ => paymentType.ToString()
    };
}
