// src/features/auth/api/auth.api.ts

import { apiClient } from "@/shared/lib/api-client";
import type { ApiResponse } from "@/shared/types/api-response.type";

import type {
    AuthResponse,
    LoginRequest,
    RegisterRequest,
    ResendOtpRequest,
    VerifyOtpRequest,
} from "../types/auth.type";

export const authApi = {
    register: async (data: RegisterRequest) => {
        const res = await apiClient.post<ApiResponse<AuthResponse>>("/auth/register", data);
        return res.data;
    },

    login: async (data: LoginRequest) => {
        const res = await apiClient.post<ApiResponse<AuthResponse>>(
            "/auth/login",
            data
        );

        return res.data;
    },

    verifyOtp: async (data: VerifyOtpRequest) => {
        const res = await apiClient.post("/auth/verify-otp", data);
        return res.data;
    },

    resendOtp: async (data: ResendOtpRequest) => {
        const res = await apiClient.post("/auth/resend-otp", data);
        return res.data;
    },
    forgotPassword: async (data: { email: string }) => {
        const res = await apiClient.post<ApiResponse<null>>(
            "/auth/forgot-password",
            data
        );

        return res.data;
    },
    resetPassword: async (data: {
        email: string;
        otpCode: string;
        newPassword: string;
        confirmPassword: string;
    }) => {
        const res = await apiClient.post<ApiResponse<null>>(
            "/auth/reset-password",
            data
        );

        return res.data;
    },
};
