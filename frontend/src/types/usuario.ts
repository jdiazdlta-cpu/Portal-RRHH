export type UsuarioList = {
  usuarioId: number;
  nombreUsuario: string;
  email: string;
  rol: string;
  isActive: boolean;
  ultimoAcceso: string | null;
  createdAt: string;
};

export type UsuarioDetail = {
  usuarioId: number;
  nombreUsuario: string;
  email: string;
  rolId: number;
  rol: string;
  isActive: boolean;
  ultimoAcceso: string | null;
  createdAt: string;
  updatedAt: string | null;
  createdBy: string | null;
  updatedBy: string | null;
};

export type CreateUsuarioRequest = {
  nombreUsuario: string;
  email: string;
  password: string;
  rolId: number;
  isActive: boolean;
};

export type UpdateUsuarioRequest = {
  nombreUsuario: string;
  email: string;
  rolId: number;
  isActive: boolean;
};

export type ResetPasswordRequest = {
  password: string;
};

export type UsuarioFormValues = {
  nombreUsuario: string;
  email: string;
  password: string;
  rolId: string;
  isActive: boolean;
};
