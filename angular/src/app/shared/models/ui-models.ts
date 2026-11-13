export interface NavigationItem {
  route: string;
  labelKey: string;
  icon?: string;
  requiredPermission?: string;
  allowedRoles?: string[];
  children?: NavigationItem[];
  badgeCount?: number;
  badgeVariant?: 'accent' | 'danger' | 'warning' | 'info';
  isExpanded?: boolean;
}

export interface UserHeaderContext {
  isAuthenticated: boolean;
  fullName: string;
  email: string;
  userName: string;
  avatarUrl?: string;
  roles: string[];
  tenantName?: string | null;
  tenantId?: string | null;
}

export interface LanguageOption {
  cultureName: string;
  displayName: string;
  shortCode: string;
  flagIcon: string;
}

export type ColumnAlignment = 'left' | 'center' | 'right';

export interface BhDataTableColumn<T = any> {
  field: keyof T | string;
  headerKey: string;
  sortable?: boolean;
  width?: string;
  align?: ColumnAlignment;
  cellTemplate?: string;
}

export interface BhPaginationState {
  currentPage: number;
  pageSize: number;
  totalCount: number;
  sortField?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface BhPageChangeEvent {
  page: number;
  pageSize: number;
}

export type ModalSize = 'sm' | 'md' | 'lg' | 'xl';
export type ModalButtonVariant = 'primary' | 'danger' | 'warning' | 'secondary' | 'accent';

export interface BhModalConfig {
  titleKey: string;
  messageKey?: string;
  size?: ModalSize;
  iconClass?: string;
  confirmTextKey?: string;
  cancelTextKey?: string;
  confirmVariant?: ModalButtonVariant;
  hideCancelButton?: boolean;
}

export type StatusBadgeVariant =
  | 'available'
  | 'dirty'
  | 'occupied'
  | 'maintenance'
  | 'active'
  | 'inactive'
  | 'locked'
  | 'pending';

export interface BhStatusBadgeConfig {
  variant: StatusBadgeVariant;
  textKey: string;
  iconClass?: string;
  isDot?: boolean;
}

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface BhToastMessage {
  id: string;
  type: ToastType;
  titleKey?: string;
  messageKey: string;
  messageParams?: any[];
  durationMs: number;
  timestamp: Date;
}

export type RoomRackStatus = 'Available' | 'Dirty' | 'Occupied' | 'Maintenance';

export interface RoomRackCardItem {
  roomId: string;
  roomNumber: string;
  roomTypeNameKey: string;
  floor: number;
  status: RoomRackStatus;
  pricePerNight: number;
  guestName?: string;
  checkOutDate?: string;
  capacity: number;
}

export interface SearchRoomCriteria {
  destinationOrHotelId: string;
  checkInDate: string;
  checkOutDate: string;
  adults: number;
  children: number;
  rooms: number;
}
