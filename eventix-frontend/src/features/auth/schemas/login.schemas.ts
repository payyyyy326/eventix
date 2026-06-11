import { z } from "zod";

export const loginSchema = z.object({
    email: z
        .email("Email không hợp lệ")
        .min(1, "Email không được để trống"),

    password: z
        .string()
        .min(1, "Mật khẩu không được để trống"),
});

export type LoginSchema = z.infer<typeof loginSchema>;