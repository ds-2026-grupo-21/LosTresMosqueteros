
export interface UpdateUserPreferenceDto {
  showAlerts?: boolean;
  photoDataUrl?: string | null;
  watchedItems?: string[];
}

export interface UserPreferenceDto {
  showAlerts?: boolean;
  photoDataUrl?: string | null;
  watchedItems?: string[];
}
