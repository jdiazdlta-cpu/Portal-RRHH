export function formatDate(value?: string | null) {
  if (!value) return 'N/D';
  return new Intl.DateTimeFormat('es-PA', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value));
}

export function formatMoney(value?: number | null) {
  return new Intl.NumberFormat('es-PA', { style: 'currency', currency: 'USD' }).format(value ?? 0);
}

export function statusClass(value: string) {
  const normalized = value.toLowerCase();
  if (normalized.includes('vencida') || normalized.includes('cesante') || normalized.includes('suspendido')) return 'danger';
  if (normalized.includes('pendiente') || normalized.includes('vacaciones')) return 'warning';
  if (normalized.includes('gestionada') || normalized.includes('activo') || normalized.includes('servicio')) return 'success';
  return 'neutral';
}
