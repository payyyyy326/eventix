// src/shared/lib/auth-storage.ts

export const authStorage = {
    getToken: () => {
        if (typeof window === "undefined") return null;
        return localStorage.getItem("accessToken");
    },

    getRefreshToken: () => {
        if (typeof window === "undefined") return null;
        return localStorage.getItem("refreshToken");
    },

    getUser: () => {
        if (typeof window === "undefined") return null;

        const user = localStorage.getItem("user");
        return user ? JSON.parse(user) : null;
    },

    setAuth: (token: string, refreshToken: string, user: unknown) => {
        localStorage.setItem("accessToken", token);
        localStorage.setItem("refreshToken", refreshToken);
        localStorage.setItem("user", JSON.stringify(user));
    },

    clear: () => {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        localStorage.removeItem("user");
    },
};