export const formatDate = (date: string | Date | null | undefined): string => {
  if (!date) return 'N/A';
  
  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(dateObj.getTime())) return 'Invalid Date';
    
    return dateObj.toLocaleDateString();
  } catch {
    return 'Invalid Date';
  }
};

export const formatDateTime = (date: string | Date | null | undefined): string => {
  if (!date) return 'N/A';
  
  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(dateObj.getTime())) return 'Invalid Date';
    
    return dateObj.toLocaleString();
  } catch {
    return 'Invalid Date';
  }
};

export const formatRelativeTime = (date: string | Date | null | undefined): string => {
  if (!date) return 'N/A';
  
  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(dateObj.getTime())) return 'Invalid Date';
    
    const now = new Date();
    const diffMs = now.getTime() - dateObj.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 30) return `${diffDays}d ago`;
    
    return dateObj.toLocaleDateString();
  } catch {
    return 'Invalid Date';
  }
};

export const isDateValid = (date: string | Date | null | undefined): boolean => {
  if (!date) return false;
  
  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return !isNaN(dateObj.getTime());
  } catch {
    return false;
  }
};