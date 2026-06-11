"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { authApi } from "../api/auth.api";
import { getErrorMessage } from "@/shared/lib/error";

export default function RegisterForm() {
    const router = useRouter();

    const [fullName, setFullName] = useState("");
    const [email, setEmail] = useState("");
    const [phoneNumber, setPhoneNumber] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");

    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();

        try {
            setLoading(true);

            const res = await authApi.register({
                fullName,
                email,
                phoneNumber,
                password,
                confirmPassword,
            });

            alert(res.message);
            router.push(`/verify-otp?email=${email}`);
        } catch (error) {
            alert(getErrorMessage(error));
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="w-full max-w-lg rounded-2xl border border-white/10 bg-white/10 p-8 shadow-2xl backdrop-blur-xl">
            <div className="mb-8 text-center">
                <h1 className="text-3xl font-bold text-white">Tạo tài khoản</h1>
                <p className="mt-2 text-sm text-slate-300">
                    Tham gia Eventix để đặt vé và quản lý sự kiện
                </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <input
                    className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                    placeholder="Họ tên"
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                />

                <input
                    className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                    placeholder="Email"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                />

                <input
                    className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                    placeholder="Số điện thoại"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                />

                <input
                    className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                    placeholder="Mật khẩu"
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />

                <input
                    className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                    placeholder="Nhập lại mật khẩu"
                    type="password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                />

                <button
                    disabled={loading}
                    className="w-full rounded-xl bg-gradient-to-r from-violet-600 to-blue-600 py-3 font-semibold text-white shadow-lg shadow-violet-500/25 transition hover:from-violet-500 hover:to-blue-500 disabled:cursor-not-allowed disabled:opacity-60"
                >
                    {loading ? "Đang đăng ký..." : "Đăng ký"}
                </button>
            </form>

            <p className="mt-6 text-center text-sm text-slate-400">
                Đã có tài khoản?{" "}
                <a href="/login" className="font-medium text-violet-300 hover:text-violet-200">
                    Đăng nhập
                </a>
            </p>
        </div>
    );
}