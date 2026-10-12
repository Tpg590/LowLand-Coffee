using FluentValidation;
using Order.API.DTOs;
using Order.API.Entities;

namespace Order.API.Validators;

public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Mã sản phẩm không được để trống.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Số lượng món phải lớn hơn 0.")
            .LessThanOrEqualTo(100).WithMessage("Số lượng món không được vượt quá 100.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Đơn giá không được âm.");

        RuleFor(x => x.Note)
            .MaximumLength(200).WithMessage("Ghi chú cho từng món tối đa 200 ký tự.")
            .Must(NotContainDangerousInput).WithMessage("Ghi chú chứa ký tự hoặc mã độc không hợp lệ.");

        RuleFor(x => x.OptionsSummary)
            .MaximumLength(500).WithMessage("Tóm tắt tùy chọn tối đa 500 ký tự.")
            .Must(NotContainDangerousInput).WithMessage("Tùy chọn chứa ký tự không hợp lệ.");
    }

    private static bool NotContainDangerousInput(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        var lower = value.ToLowerInvariant();
        return !lower.Contains("<script") && !lower.Contains("javascript:") && !lower.Contains("onerror=") && !lower.Contains("onload=");
    }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Đơn hàng phải có ít nhất 1 món.")
            .Must(items => items.Count <= 50).WithMessage("Một đơn hàng tối đa 50 món.");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateOrderItemRequestValidator());

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Số tiền giảm giá không được âm.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Phương thức thanh toán không được để trống.")
            .Must(m => PaymentMethod.IsValid(m))
            .WithMessage("Phương thức thanh toán không hợp lệ (hỗ trợ CASH, VNPAY, PAYOS).");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú đơn hàng tối đa 500 ký tự.")
            .Must(NotContainDangerousInput).WithMessage("Ghi chú chứa ký tự không hợp lệ.");
    }

    private static bool NotContainDangerousInput(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        var lower = value.ToLowerInvariant();
        return !lower.Contains("<script") && !lower.Contains("javascript:") && !lower.Contains("onerror=") && !lower.Contains("onload=");
    }
}

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Trạng thái đơn hàng không được để trống.")
            .Must(s => OrderStatus.IsValid(s))
            .WithMessage("Trạng thái không hợp lệ (hỗ trợ PENDING, PROCESSING, SHIPPING, COMPLETED, CANCELLED).");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú cập nhật tối đa 500 ký tự.")
            .Must(NotContainDangerousInput).WithMessage("Ghi chú chứa ký tự không hợp lệ.");
    }

    private static bool NotContainDangerousInput(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        var lower = value.ToLowerInvariant();
        return !lower.Contains("<script") && !lower.Contains("javascript:") && !lower.Contains("onerror=") && !lower.Contains("onload=");
    }
}

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do hủy đơn không được để trống.")
            .MaximumLength(500).WithMessage("Lý do hủy đơn tối đa 500 ký tự.")
            .Must(NotContainDangerousInput).WithMessage("Lý do hủy chứa ký tự không hợp lệ.");
    }

    private static bool NotContainDangerousInput(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        var lower = value.ToLowerInvariant();
        return !lower.Contains("<script") && !lower.Contains("javascript:") && !lower.Contains("onerror=") && !lower.Contains("onload=");
    }
}
