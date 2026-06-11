export type UserProfileResponse = {
    id: string;
    email: string;
    passwordHash?: string;
    fullName?: string | null;
    phoneNumber?: string | null;
    avatarUrl?: string | null;
    status: string;
    createdAt: string;
    updatedAt?: string | null;
    emailVerified: boolean;
    emailVerifiedAt?: string | null;
    roles: string[];
};

export type UpdateProfileRequest = {
    fullName: string;
    phoneNumber: string;
    avatar?: File | null;
};

export type ChangePasswordRequest = {
    oldPassword: string;
    newPassword: string;
    confirmPassword: string;
};