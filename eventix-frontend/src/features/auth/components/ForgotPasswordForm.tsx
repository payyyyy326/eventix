"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { authApi } from "../api/auth.api";
import { getErrorMessage } from "@/shared/lib/error";

export default function ForgotPasswordForm() {
    const router = useRouter();

    const [email, setEmail] = useState("");
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();

        try {
            setLoading(true);

            const res = await authApi.forgotPassword({ email });

            alert(res.message);

            router.push(`/reset-password?email=${email}`);
        } catch (error) {
            alert(getErrorMessage(error));
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="w-full max-w-md rounded-2xl border border-white/10 bg-white/10 p-8 shadow-2xl backdrop-blur-xl">
            <div className="mb-8 text-center">
                <h1 className="text-3xl font-bold text-white">Quên mật khẩu</h1>
                <p className="mt-2 text-sm text-slate-300">
                    Nhập email để nhận mã OTP đặt lại mật khẩu
                </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div>
                    <label className="mb-2 block text-sm font-medium text-slate-200">
                        Email
                    </label>

                    <input
                        className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none transition placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                        placeholder="example@gmail.com"
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                    />
                </div>

                <button
                    disabled={loading}
                    className="w-full rounded-xl bg-gradient-to-r from-violet-600 to-blue-600 py-3 font-semibold text-white shadow-lg shadow-violet-500/25 transition hover:from-violet-500 hover:to-blue-500 disabled:cursor-not-allowed disabled:opacity-60"
                >
                    {loading ? "Đang gửi..." : "Gửi mã OTP"}
                </button>
            </form>

            <p className="mt-6 text-center text-sm text-slate-400">
                Nhớ mật khẩu?{" "}
                <a href="/login" className="font-medium text-violet-300 hover:text-violet-200">
                    Đăng nhập
                </a>
            </p>
        </div>
    );
}