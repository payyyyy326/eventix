// src/shared/lib/error.ts

import axios from "axios";

type ApiErrorResponse = {
    code?: string;
    message?: string;
    isSuccess?: boolean;
    data?: unknown;
};

export function getErrorMessage(error: unknown): string {
    if (axios.isAxiosError<ApiErrorResponse>(error)) {
        return (
            error.response?.data?.message ??
            error.message ??
            "Có lỗi xảy ra"
        );
    }

    return "Có lỗi xảy ra";
}