export type AuthUser = {
  userId: number;
  email: string;
  nombreUsuario: string;
  rol: string;
};

export type LoginResponse = {
  token: string;
  expiresAt: string;
  user: AuthUser;
};
