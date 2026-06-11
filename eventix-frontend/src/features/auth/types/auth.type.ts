// src/features/auth/types/auth.type.ts

export type RegisterRequest = {
    fullName?: string;
    email: string;
    phoneNumber: string;
    password: string;
    confirmPassword: string;
};

export type LoginRequest = {
    email: string;
    password: string;
};

export type VerifyOtpRequest = {
    email: string;
    otpCode: string;
};

export type ResendOtpRequest = {
    email: string;
};

export type AuthUser = {
    id: string;
    email: string;
    fullName?: string;
    avatarUrl?: string | null;
    roleID?: number;
    emailVerified: boolean;
};

export type AuthResponse = {
    token: string;
    accessTokenExpiresAt: string;
    refreshToken: string;
    refreshTokenExpiresAt: string;
    user: AuthUser;
};