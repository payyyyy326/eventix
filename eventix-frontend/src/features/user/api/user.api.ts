import { apiClient } from "@/shared/lib/api-client";
import type { ApiResponse } from "@/shared/types/api-response.type";
import type {
    ChangePasswordRequest,
    UpdateProfileRequest,
    UserProfileResponse,
} from "../types/user.type";

export const userApi = {
    getProfile: async () => {
        const res = await apiClient.get<ApiResponse<UserProfileResponse>>(
            "/user/profile"
        );

        return res.data;
    },

    updateProfile: async (data: UpdateProfileRequest) => {
        const formData = new FormData();

        formData.append("FullName", data.fullName);
        formData.append("PhoneNumber", data.phoneNumber);

        if (data.avatar) {
            formData.append("Avatar", data.avatar);
        }

        const res = await apiClient.put<ApiResponse<UserProfileResponse>>(
            "/user/profile",
            formData,
            {
                headers: {
                    "Content-Type": "multipart/form-data",
                },
            }
        );

        return res.data;
    },

    changePassword: async (data: ChangePasswordRequest) => {
        const res = await apiClient.post<ApiResponse<null>>(
            "/user/change-password",
            data
        );

        return res.data;
    },
};