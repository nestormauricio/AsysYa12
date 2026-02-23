import axios from 'axios';

const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

const api = axios.create({ baseURL: BASE_URL });

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export interface AuthResult {
  token: string;
  expiresAt: string;
  username: string;
  email: string;
  role: string;
}

export interface Provider {
  id: string;
  name: string;
  phoneNumber: string | null;
  type: string;
  isAvailable: boolean;
  rating: number;
  activeAssignments: number;
  totalAssignments: number;
  latitude: number;
  longitude: number;
}

export interface OptimizationResult {
  providerId: string;
  providerName: string;
  providerPhone: string | null;
  score: number;
  ratingComponent: number;
  availabilityComponent: number;
  distanceComponent: number;
  etaComponent: number;
  distanceKm: number;
  estimatedMinutes: number;
  estimatedArrival: string;
}

export const authService = {
  login: (email: string, password: string) =>
    api.post<AuthResult>('/api/auth/login', { email, password }),
  register: (username: string, email: string, password: string) =>
    api.post<AuthResult>('/api/auth/register', { username, email, password }),
};

export const providerService = {
  getAvailable: (type?: string) =>
    api.get<Provider[]>('/api/providers/available', { params: type ? { type } : {} }),
  getAll: () => api.get<Provider[]>('/api/providers'),
};

export const optimizationService = {
  optimize: (latitude: number, longitude: number, requiredType?: string) =>
    api.post<OptimizationResult[]>('/optimize', { latitude, longitude, requiredType }),
};

export default api;
