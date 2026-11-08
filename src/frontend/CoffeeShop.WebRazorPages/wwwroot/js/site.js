// =========================================================================
// LowLand Coffee - Client Utilities & Flowbite Toast Notification System
// Quy tắc UI 1 & 2: Thay thế hoàn toàn alert() bằng Toast và dùng Flaticon Icon
// =========================================================================

/**
 * Hiển thị Flowbite Toast thông báo đẹp mắt thay thế hoàn toàn alert()
 * @param {string} title Tiêu đề thông báo
 * @param {string} message Nội dung chi tiết
 * @param {'success'|'error'|'warning'|'info'} type Loại thông báo
 * @param {number} duration Thời gian hiển thị (ms)
 */
function showToast(title, message = '', type = 'success', duration = 3500) {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }

    // Cấu hình màu sắc & Flaticon Web Icons theo chuẩn Flowbite Toast
    const config = {
        success: {
            bg: 'bg-white border-l-4 border-emerald-500',
            iconBg: 'bg-emerald-100 text-emerald-600',
            icon: '<i class="fi fi-rr-check text-base"></i>'
        },
        error: {
            bg: 'bg-white border-l-4 border-red-highland',
            iconBg: 'bg-red-100 text-red-highland',
            icon: '<i class="fi fi-rr-cross-small text-lg"></i>'
        },
        warning: {
            bg: 'bg-white border-l-4 border-amber-500',
            iconBg: 'bg-amber-100 text-amber-600',
            icon: '<i class="fi fi-rr-exclamation text-base"></i>'
        },
        info: {
            bg: 'bg-white border-l-4 border-blue-500',
            iconBg: 'bg-blue-100 text-blue-600',
            icon: '<i class="fi fi-rr-info text-base"></i>'
        }
    }[type] || {
        bg: 'bg-white border-l-4 border-blue-500',
        iconBg: 'bg-blue-100 text-blue-600',
        icon: '<i class="fi fi-rr-info text-base"></i>'
    };

    const toast = document.createElement('div');
    toast.setAttribute('role', 'alert');
    toast.className = `toast-item flex items-start w-full max-w-sm p-4 text-gray-700 rounded-2xl shadow-2xl border border-gray-100 ${config.bg} min-w-[320px] gap-3`;
    toast.innerHTML = `
        <div class="inline-flex items-center justify-center flex-shrink-0 w-9 h-9 rounded-xl ${config.iconBg}">
            ${config.icon}
        </div>
        <div class="flex-1 text-sm font-normal">
            <span class="mb-1 text-sm font-extrabold text-gray-900 block leading-tight">${title}</span>
            ${message ? `<div class="text-xs font-medium text-gray-600 leading-snug mt-0.5">${message}</div>` : ''}
        </div>
        <button type="button" class="ms-auto -mx-1.5 -my-1.5 bg-white text-gray-400 hover:text-gray-900 rounded-lg focus:ring-2 focus:ring-gray-300 p-1.5 hover:bg-gray-100 inline-flex items-center justify-center h-8 w-8 transition" aria-label="Close" onclick="this.closest('.toast-item').remove()">
            <span class="sr-only">Đóng</span>
            <i class="fi fi-rr-cross-small text-base"></i>
        </button>
    `;

    container.appendChild(toast);

    // Kích hoạt animation slide-in
    requestAnimationFrame(() => {
        toast.classList.add('toast-show');
    });

    // Tự động đóng sau duration ms
    setTimeout(() => {
        toast.classList.remove('toast-show');
        setTimeout(() => toast.remove(), 400);
    }, duration);
}

// Thay thế hoàn toàn alert, prompt mặc định của trình duyệt sang Toast thông báo đẹp mắt
window.alert = function (msg) {
    if (typeof showToast === 'function') {
        showToast('Thông báo hệ thống', String(msg), 'info');
    }
};

window.prompt = function (msg, defaultVal) {
    if (typeof showToast === 'function') {
        showToast('Yêu cầu nhập liệu', String(msg), 'info');
    }
    return defaultVal || '';
};

// =========================================================================
// Quản lý Giỏ hàng Phân lập theo từng Khách hàng (User-isolated Cart)
// =========================================================================
function getCurrentUser() {
    try {
        const userStr = localStorage.getItem('current_user');
        return userStr ? JSON.parse(userStr) : null;
    } catch {
        return null;
    }
}

function getUserCartKey() {
    const user = getCurrentUser();
    if (!user) return null;
    const userKey = user.id || user.email || user.phoneNumber || user.phone || 'customer';
    return 'LOWLAND_CART_' + userKey.toString().toLowerCase().trim().replace(/[^a-z0-9_-]/g, '_');
}

function isUserLoggedIn() {
    return !!getCurrentUser();
}

function getCart() {
    const cartKey = getUserCartKey();
    try {
        if (cartKey) {
            const data = localStorage.getItem(cartKey);
            if (data) {
                const parsed = JSON.parse(data);
                if (Array.isArray(parsed) && parsed.length > 0) {
                    return parsed;
                }
            }
        }
        const fallback = localStorage.getItem('lowland_cart');
        if (fallback) {
            const parsedFallback = JSON.parse(fallback);
            if (Array.isArray(parsedFallback)) {
                if (cartKey && parsedFallback.length > 0) {
                    localStorage.setItem(cartKey, JSON.stringify(parsedFallback));
                }
                return parsedFallback;
            }
        }
        return [];
    } catch {
        return [];
    }
}

function saveCart(cart) {
    const safeCart = Array.isArray(cart) ? cart : [];
    const cartKey = getUserCartKey();
    if (cartKey) {
        localStorage.setItem(cartKey, JSON.stringify(safeCart));
    }
    localStorage.setItem('lowland_cart', JSON.stringify(safeCart));
    updateCartBadge();
}

function clearUserCart() {
    const cartKey = getUserCartKey();
    if (cartKey) {
        localStorage.removeItem(cartKey);
    }
    localStorage.removeItem('lowland_cart');
    updateCartBadge();
}

function updateCartBadge() {
    const badges = document.querySelectorAll('.cart-badge');
    if (!isUserLoggedIn()) {
        badges.forEach(badge => {
            badge.style.display = 'none';
            badge.textContent = '0';
        });
        return;
    }

    const cart = getCart();
    const count = cart.reduce((sum, item) => sum + (item.quantity || 1), 0);
    badges.forEach(badge => {
        badge.textContent = count;
        badge.style.display = count > 0 ? 'inline-flex' : 'none';
    });
}

function addToCart(item) {
    if (!isUserLoggedIn()) {
        showToast('Yêu cầu đăng nhập', 'Vui lòng đăng nhập tài khoản thành viên để thêm món vào giỏ hàng!', 'warning', 3500);
        setTimeout(() => {
            window.location.href = '/Auth/Login';
        }, 1200);
        return;
    }

    const cart = getCart();
    const existing = cart.find(x => (x.id && item.id && x.id === item.id && x.size === item.size) || (x.name === item.name && x.size === item.size));
    if (existing) {
        existing.quantity += item.quantity || 1;
    } else {
        cart.push({
            id: item.id || '',
            name: item.name,
            size: item.size || 'Size M',
            price: Number(item.price) || 0,
            quantity: item.quantity || 1,
            image: item.image || '/images/logo.jpg'
        });
    }
    saveCart(cart);
    showToast('Thêm vào giỏ thành công', `Đã thêm món "${item.name}" (${item.size || 'Size M'}) vào giỏ hàng`, 'success');
}

document.addEventListener('DOMContentLoaded', () => {
    updateCartBadge();
});
