// =========================================================================
// LowLand Coffee - Client Utilities & Toast Notification System
// =========================================================================

/**
 * Hiển thị Toast thông báo đẹp mắt thay thế hoàn toàn alert()
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

    // Cấu hình màu sắc & icon theo loại toast
    const config = {
        success: {
            bg: 'bg-white border-l-4 border-emerald-500',
            text: 'text-emerald-800',
            iconBg: 'bg-emerald-100 text-emerald-600',
            icon: `<svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"></path></svg>`
        },
        error: {
            bg: 'bg-white border-l-4 border-red-highland',
            text: 'text-red-800',
            iconBg: 'bg-red-100 text-red-highland',
            icon: `<svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"></path></svg>`
        },
        warning: {
            bg: 'bg-white border-l-4 border-amber-500',
            text: 'text-amber-800',
            iconBg: 'bg-amber-100 text-amber-600',
            icon: `<svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>`
        },
        info: {
            bg: 'bg-white border-l-4 border-blue-500',
            text: 'text-blue-800',
            iconBg: 'bg-blue-100 text-blue-600',
            icon: `<svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>`
        }
    }[type] || config.info;

    const toast = document.createElement('div');
    toast.className = `toast-item flex items-start gap-3 p-4 rounded-xl shadow-xl border border-gray-100 ${config.bg} min-w-[320px] max-w-sm`;
    toast.innerHTML = `
        <div class="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center ${config.iconBg}">
            ${config.icon}
        </div>
        <div class="flex-1 text-sm">
            <h4 class="font-semibold text-gray-900 leading-tight">${title}</h4>
            ${message ? `<p class="mt-1 text-gray-600 leading-snug">${message}</p>` : ''}
        </div>
        <button type="button" class="flex-shrink-0 text-gray-400 hover:text-gray-600 p-1" onclick="this.closest('.toast-item').remove()">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
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
        showToast('Thông báo', String(msg), 'info');
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
    // Phân lập giỏ hàng theo định danh ID hoặc Email hoặc SĐT của từng người dùng
    const userKey = user.id || user.email || user.phoneNumber || user.phone || 'customer';
    return 'LOWLAND_CART_' + userKey.toString().toLowerCase().trim().replace(/[^a-z0-9_-]/g, '_');
}

function isUserLoggedIn() {
    return !!getCurrentUser();
}

function getCart() {
    // Chỉ người dùng đã đăng nhập mới nạp giỏ hàng của chính họ
    const cartKey = getUserCartKey();
    if (!cartKey) {
        return [];
    }
    try {
        const data = localStorage.getItem(cartKey);
        return data ? JSON.parse(data) : [];
    } catch {
        return [];
    }
}

function saveCart(cart) {
    const cartKey = getUserCartKey();
    if (!cartKey) {
        updateCartBadge();
        return;
    }
    localStorage.setItem(cartKey, JSON.stringify(cart));
    updateCartBadge();
}

function clearUserCart() {
    const cartKey = getUserCartKey();
    if (cartKey) {
        localStorage.removeItem(cartKey);
    }
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
    // Bắt buộc phải đăng nhập mới được thêm vào giỏ hàng
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
    showToast('Thêm vào giỏ thành công', `Đã thêm món "${item.name}" vào giỏ hàng`, 'success');
}

document.addEventListener('DOMContentLoaded', () => {
    updateCartBadge();
});
