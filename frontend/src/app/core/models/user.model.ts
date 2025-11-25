export interface User {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Citizen' | 'Official' | 'Moderator' | 'Admin';
}

export interface AuthResponse {
  success: boolean;
  message: string;
  token?: string;
  user?: User;
}