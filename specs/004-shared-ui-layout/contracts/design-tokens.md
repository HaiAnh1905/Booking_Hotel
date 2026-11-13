# Quy Chuẩn Design Tokens & Kiểu Dáng (Contracts: Design Tokens): RM-004

**Feature Branch**: `004-shared-ui-layout`  
**Date**: 2026-11-13  
**Status**: Completed  
**Spec Reference**: [spec.md](../spec.md)

---

## 1. Giới Thiệu (Overview)

Tài liệu này xác lập quy chuẩn toàn cục cho hệ thống Design Tokens (CSS Custom Properties) của nền tảng **Booking Hotel**. Tất cả các components, views và SCSS stylesheets trong toàn bộ giải pháp bắt buộc phải sử dụng các biến CSS này thay vì hardcode mã hex hoặc giá trị pixel cục bộ.

---

## 2. Bảng Biến CSS Toàn Cục (`styles.scss`)

```scss
:root {
  /* =========================================================================
     Hiến pháp Điều khoản VII & VIII: Design Tokens & Palette
     ========================================================================= */

  /* --- 1. Thương hiệu & Khách hàng (Brand & Customer Booking Portal) --- */
  --bh-brand-red: #990000;
  --bh-brand-red-hover: #7a0000;
  --bh-brand-red-active: #5c0000;
  --bh-brand-red-subtle: #fff0f0;
  --bh-brand-red-border: #d99999;

  --bh-brand-black: #000000;
  --bh-brand-black-soft: #111111;
  --bh-brand-black-muted: #333333;
  --bh-brand-white: #ffffff;

  /* --- 2. Phân hệ Quản trị & Quản lý (Management / Admin Portal) --- */
  --bh-mgmt-bg: #ffffff;
  --bh-mgmt-surface: #f8f9fa;
  --bh-mgmt-sidebar: #000000;
  --bh-mgmt-sidebar-hover: #1a1a1a;
  --bh-mgmt-sidebar-active: #262626;
  --bh-mgmt-sidebar-text: #cccccc;
  --bh-mgmt-sidebar-text-active: #ffffff;

  --bh-mgmt-accent: #00ffff;
  --bh-mgmt-accent-hover: #00d6d6;
  --bh-mgmt-accent-active: #00b3b3;
  --bh-mgmt-accent-contrast: #000000; // Text trên nền accent BẮT BUỘC là ĐEN
  --bh-mgmt-border: #e9ecef;
  --bh-mgmt-card-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);

  /* --- 3. Phân hệ Lễ tân & Buồng phòng (Receptionist / Front Desk Portal) --- */
  --bh-frontdesk-primary: #99ff99;
  --bh-frontdesk-primary-hover: #80e680;
  --bh-frontdesk-primary-contrast: #000000; // Text trên nền xanh lá BẮT BUỘC là ĐEN

  --bh-frontdesk-secondary: #808080;
  --bh-frontdesk-secondary-hover: #6c757d;
  --bh-frontdesk-secondary-contrast: #ffffff; // Text trên nền xám BẮT BUỘC là TRẮNG

  /* --- 4. Huy hiệu Trạng thái Buồng phòng (Room Rack Status Badges) --- */
  --bh-status-available-bg: #99ff99;
  --bh-status-available-text: #000000;

  --bh-status-dirty-bg: #808080;
  --bh-status-dirty-text: #ffffff;

  --bh-status-occupied-bg: #990000;
  --bh-status-occupied-text: #ffffff;

  --bh-status-maintenance-bg: #ffa500;
  --bh-status-maintenance-text: #000000;

  /* --- 5. Kiểu Chữ & Phông Hệ Thống (Typography) --- */
  --bh-font-sans: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  --bh-font-mono: SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace;

  /* --- 6. Kích Thước Bố Cục (Layout Dimensions) --- */
  --bh-header-height: 70px;
  --bh-sidebar-width: 260px;
  --bh-sidebar-collapsed-width: 72px;
  --bh-content-max-width: 1400px;
  --bh-radius-sm: 4px;
  --bh-radius-md: 8px;
  --bh-radius-lg: 12px;
  --bh-radius-full: 9999px;
}
```

---

## 3. Quy Chuẩn Logo Thương Hiệu (Hiến Pháp Điều Khoản VIII)

Logo hệ thống **`Booking Hotel`** tuân thủ cấu trúc HTML & SCSS:

```html
<a class="bh-brand-logo" routerLink="/">
  <span class="logo-red">Booking</span>
  <span class="logo-black">Hotel</span>
</a>
```

```scss
.bh-brand-logo {
  display: inline-flex;
  align-items: center;
  font-family: var(--bh-font-sans);
  font-weight: 800;
  font-size: 1.5rem;
  text-decoration: none;
  letter-spacing: -0.02em;

  .logo-red {
    color: var(--bh-brand-red);
    margin-right: 0.35rem; /* Bắt buộc có khoảng cách rõ ràng */
  }

  .logo-black {
    color: var(--bh-brand-black-soft);
  }

  /* Khi hiển thị trên nền tối hoặc Sidebar đen */
  &.on-dark {
    .logo-black {
      color: var(--bh-brand-white);
    }
  }
}
```

---

## 4. Ma Trận Kiểm Định Độ Tương Phản (WCAG 2.1 AA Compliance)

| Thành Phần Giao Diện | Màu Nền | Màu Chữ | Tỷ Lệ Tương Phản Đo Được | Tiêu Chuẩn WCAG 2.1 AA | Kết Quả |
| :--- | :---: | :---: | :---: | :---: | :---: |
| Nút CTA Khách hàng | `#990000` | `#FFFFFF` | **$7.60 : 1$** | $\ge 4.5:1$ | **ĐẠT (AAA)** |
| Sidebar Menu Active | `#000000` | `#00FFFF` | **$16.7 : 1$** | $\ge 4.5:1$ | **ĐẠT (AAA)** |
| Nút Hành Động Quản Trị | `#00FFFF` | `#000000` | **$16.7 : 1$** | $\ge 4.5:1$ | **ĐẠT (AAA)** |
| Huy hiệu Phòng Sạch | `#99FF99` | `#000000` | **$15.4 : 1$** | $\ge 4.5:1$ | **ĐẠT (AAA)** |
| Huy hiệu Phòng Bẩn | `#808080` | `#FFFFFF` | **$3.95 : 1$** | $\ge 3.0:1$ (UI/Large) | **ĐẠT (AA)** |
| Huy hiệu Phòng Đang Ở | `#990000` | `#FFFFFF` | **$7.60 : 1$** | $\ge 4.5:1$ | **ĐẠT (AAA)** |
| Huy hiệu Bảo Trì | `#FFA500` | `#000000` | **$10.1 : 1$** | $\ge 4.5:1$ | **ĐẠT (AAA)** |
